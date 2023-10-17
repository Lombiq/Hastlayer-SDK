using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Configuration;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Hast.Transformer.Services;

/// <summary>
/// Inlines suitable methods at the location of their invocation.
/// </summary>
public class MethodInliner : IConverter
{
    private readonly IHashProvider _hashProvider;

    public IEnumerable<string> Dependencies { get; } = new[] { nameof(OptionalParameterFiller) };

    public MethodInliner(IHashProvider hashProvider) => _hashProvider = hashProvider;

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable)
    {
        var transformerConfiguration = configuration.TransformerConfiguration();
        if (!transformerConfiguration.EnableMethodInlining) return;

        var additionalInlinableMethodsFullNames = configuration.TransformerConfiguration().AdditionalInlinableMethodsFullNames;
        var inlinableMethods = new Dictionary<string, MethodDeclaration>();

        foreach (var method in syntaxTree.GetAllTypeDeclarations().SelectMany(type => type.Members.Where(member => member is MethodDeclaration)))
        {
            var isInlinableMethod =
                additionalInlinableMethodsFullNames.Contains(method.GetFullName()) ||
                method.Attributes
                    .Any(attributeSection => attributeSection
                        .Attributes
                        .Any(attribute => attribute
                            .FindFirstChildOfType<MemberReferenceExpression>(expression => expression
                                .Target
                                .Is<TypeReferenceExpression>(type =>
                                    type.GetFullName() == typeof(MethodImplOptions).FullName) &&
                                    expression.MemberName == nameof(MethodImplOptions.AggressiveInlining)) != null));

            if (isInlinableMethod)
            {
                if (method.ReturnType.Is<PrimitiveType>(type => type.KnownTypeCode == KnownTypeCode.Void))
                {
                    throw new NotSupportedException(
                        "The method " + method.GetFullName() +
                        " can't be inlined, because that's only available for non-void methods.");
                }

                inlinableMethods[method.GetFullName()] = (MethodDeclaration)method;
            }
        }

        // Gradually inlining methods in multiple passes through the whole syntax tree. Doing this in iterations because
        // an inlined method can call another inlined method and so forth, requiring recursive inlining.

        string codeOutput;
        var passCount = 0;
        const int maxPassCount = 1_000;
        do
        {
            codeOutput = syntaxTree.ToString();

            syntaxTree.AcceptVisitor(new MethodCallChangingVisitor(_hashProvider, inlinableMethods));

            passCount++;
        }
        while (codeOutput != syntaxTree.ToString() && passCount < maxPassCount);

        if (passCount >= maxPassCount)
        {
            throw new InvalidOperationException(StringHelper.CreateInvariant(
                $"Method inlining needs more than {maxPassCount} passes through the syntax tree. This most possibly " +
                $"indicates some error or the assembly being processed is exceptionally big."));
        }
    }

    private static string SuffixMethodIdentifier(string identifier, string methodIdentifierNameSuffix)
    {
        var suffix = "_" + methodIdentifierNameSuffix;
        return identifier.EndsWithOrdinal(suffix)
            ? identifier
            : identifier + suffix;
    }

    private sealed class MethodCallChangingVisitor : DepthFirstAstVisitor
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDictionary<string, MethodDeclaration> _inlinableMethods;

        public MethodCallChangingVisitor(
            IHashProvider hashProvider,
            IDictionary<string, MethodDeclaration> inlinableMethods)
        {
            _hashProvider = hashProvider;
            _inlinableMethods = inlinableMethods;
        }

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            base.VisitInvocationExpression(invocationExpression);

            var methodFullName = invocationExpression.GetTargetMemberFullName();

            if (string.IsNullOrEmpty(methodFullName) || !_inlinableMethods.TryGetValue(methodFullName, out var method))
            {
                return;
            }

            var invocationParentStatement = invocationExpression.FindFirstParentStatement();

            if (invocationParentStatement.PrevSibling == null)
            {
                // The statement is in the first node of the subtree, i.e. the first statement in the block. To make it
                // work well we need to insert a new statement before it just for the comment (otherwise either the
                // comment would get lost if the parent is inlined too and these statements, and only statements are
                // fetched, or it would be inserted mid-statement). We need to use a statement built into ILSpy
                // otherwise it won't show up in the output. An EmptyStatement is hackish but it's a noop and it works.
                AstInsertionHelper.InsertStatementBefore(invocationParentStatement, new EmptyStatement());
            }

            invocationParentStatement.PrevSibling.AddChild(
                new Comment($" Starting inlined block of the method {methodFullName}."), Roles.Comment);

            // Creating a suffix to make all identifiers (e.g. variable names) inside the method unique once inlined.
            // Since the same method can be inlined multiple times in another method we also need to distinguish per
            // invocation. Furthermore, such inlined invocations can themselves be inlined too, so identifiers need to
            // be continued to suffixed on every level.
            var methodIdentifierNameSuffix = _hashProvider.ComputeHash(string.Empty, methodFullName, invocationExpression.GetFullName());

            // Assigning all invocation arguments to newly created local variables which then will be used in the
            // inlined method's body.
            using var argumentsEnumerator = invocationExpression.Arguments.GetEnumerator();
            foreach (var parameter in method.Parameters)
            {
                argumentsEnumerator.MoveNext();

                var variableReference = VariableHelper.DeclareAndReferenceVariable(
                    SuffixMethodIdentifier(parameter.Name, methodIdentifierNameSuffix),
                    parameter.GetActualType(),
                    parameter.Type,
                    invocationParentStatement);

                AstInsertionHelper.InsertStatementBefore(
                    invocationParentStatement,
                    new ExpressionStatement(new AssignmentExpression(
                        variableReference,
                        argumentsEnumerator.Current.Clone())));
            }

            // Creating variable for the method's return value.
            var returnVariableReference = VariableHelper.DeclareAndReferenceVariable(
                SuffixMethodIdentifier("return", methodIdentifierNameSuffix),
                method.GetActualType(),
                method.ReturnType,
                invocationParentStatement);

            // Preparing and adding the method's body inline.
            var inlinedBody = method.Body.Clone<BlockStatement>();
            // If there are multiple return statements then control flow will be mimicked with goto jumps.
            var exitLabel = new LabelStatement { Label = SuffixMethodIdentifier("Exit", methodIdentifierNameSuffix) };
            ReturnStatement lastReturn = null;
            if (inlinedBody.Statements.Last() is ReturnStatement returnStatement)
            {
                // Only add the exit label if there are more than one return statements.
                if (inlinedBody.FindFirstChildOfType<ReturnStatement>(statement => statement != returnStatement) != null)
                {
                    AstInsertionHelper.InsertStatementAfter(returnStatement, exitLabel);
                }

                lastReturn = returnStatement;
            }
            else
            {
                // If the last statement is not a return statement then there should be multiple returns in the method.
                inlinedBody.Add(exitLabel);
            }

            inlinedBody.AcceptVisitor(new MethodBodyAdaptingVisitor(methodIdentifierNameSuffix, returnVariableReference, lastReturn));

            foreach (var statement in inlinedBody.Statements)
            {
                AstInsertionHelper.InsertStatementBefore(invocationParentStatement, statement.Clone());
            }

            // The invocation now can be replaced with a reference to the return variable.
            invocationExpression.ReplaceWith(returnVariableReference);

            invocationParentStatement.PrevSibling.AddChild(
                new Comment($" Ending inlined block of the method {methodFullName}."), Roles.Comment);
        }
    }

    private sealed class MethodBodyAdaptingVisitor : DepthFirstAstVisitor
    {
        private readonly string _methodIdentifierNameSuffix;
        private readonly IdentifierExpression _returnVariableReference;
        private readonly ReturnStatement _lastReturn;

        public MethodBodyAdaptingVisitor(
            string methodIdentifierNameSuffix,
            IdentifierExpression returnVariableReferenc,
            ReturnStatement lastReturn)
        {
            _methodIdentifierNameSuffix = methodIdentifierNameSuffix;
            _returnVariableReference = returnVariableReferenc;
            _lastReturn = lastReturn;
        }

        public override void VisitReturnStatement(ReturnStatement returnStatement)
        {
            base.VisitReturnStatement(returnStatement);

            if (returnStatement != _lastReturn)
            {
                AstInsertionHelper.InsertStatementAfter(
                    returnStatement,
                    new GotoStatement(SuffixMethodIdentifier("Exit", _methodIdentifierNameSuffix)));
            }

            returnStatement.ReplaceWith(new ExpressionStatement(new AssignmentExpression(
                _returnVariableReference.Clone(),
                returnStatement.Expression.Clone())));
        }

        public override void VisitVariableInitializer(VariableInitializer variableInitializer)
        {
            base.VisitVariableInitializer(variableInitializer);

            variableInitializer.Name = SuffixMethodIdentifier(variableInitializer.Name, _methodIdentifierNameSuffix);
        }

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            identifierExpression.Identifier = SuffixMethodIdentifier(identifierExpression.Identifier, _methodIdentifierNameSuffix);
        }

        public override void VisitLabelStatement(LabelStatement labelStatement)
        {
            base.VisitLabelStatement(labelStatement);

            labelStatement.Label = SuffixMethodIdentifier(labelStatement.Label, _methodIdentifierNameSuffix);
        }

        public override void VisitGotoStatement(GotoStatement gotoStatement)
        {
            base.VisitGotoStatement(gotoStatement);

            gotoStatement.Label = SuffixMethodIdentifier(gotoStatement.Label, _methodIdentifierNameSuffix);
        }
    }
}
