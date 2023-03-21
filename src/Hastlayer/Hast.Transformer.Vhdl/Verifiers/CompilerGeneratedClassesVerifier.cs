using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.Verifiers;

/// <summary>
/// Verifies compiler-generated classes for transformation, checking for potential issues preventing processing.
/// </summary>
public class CompilerGeneratedClassesVerifier : IVerifyer
{
    public void Verify(SyntaxTree syntaxTree, ITransformationContext transformationContext) =>
        VerifyCompilerGeneratedClasses(syntaxTree);

    /// <summary>
    /// Verifies compiler-generated classes for transformation, checking for potential issues preventing processing.
    /// </summary>
    private static void VerifyCompilerGeneratedClasses(SyntaxTree syntaxTree)
    {
        var compilerGeneratedClasses = syntaxTree
            .GetAllTypeDeclarations()
            .Where(type => type.GetFullName().IsDisplayOrClosureClassName());

        foreach (var members in compilerGeneratedClasses.Select(compilerGeneratedClass => compilerGeneratedClass.Members))
        {
            var fields = members.OfType<FieldDeclaration>()
                .OrderBy(field => field.Variables.Single().Name)
                .ToDictionary(field => field.Variables.Single().Name);

            foreach (var method in members.OfType<MethodDeclaration>())
            {
                // Adding parameters for every field that the method used, in alphabetical order, and changing field
                // references to parameter references.

                method.AcceptVisitor(new MemberReferenceExpressionVisitingVisitor(
                    expression => MemberReferenceExpressionProcessor(expression, fields, method)));
            }
        }
    }

    private static void MemberReferenceExpressionProcessor(
        MemberReferenceExpression memberReferenceExpression,
        IDictionary<string, FieldDeclaration> fields,
        MethodDeclaration method)
    {
        if (!memberReferenceExpression.IsFieldReference()) return;

        var fullName = memberReferenceExpression.GetMemberResolveResult().GetFullName();

        var field = fields
            .Values
            .SingleOrDefault(field => field.GetMemberResolveResult().GetFullName() == fullName);

        // The field won't be on the compiler-generated class if the member reference accesses a user-defined type's
        // field.
        if (field == null) return;

        // Is the field assigned to? Because we don't support that currently, since with it being converted to a
        // parameter we'd need to return its value and assign it to the caller's variable. Maybe we'll allow this with
        // static field support, but not for lambdas used in parallelized expressions (since that would require
        // concurrent access too).
        var isAssignedTo =
            // The field is directly assigned to.
            (memberReferenceExpression.Parent is AssignmentExpression directAssignment &&
            directAssignment.Left == memberReferenceExpression)
            ||
            // The field's indexed element is assigned to.
            (memberReferenceExpression.Parent is IndexerExpression &&
            memberReferenceExpression.Parent.Parent is AssignmentExpression indexedAssignment &&
            indexedAssignment.Left == memberReferenceExpression.Parent);
        if (isAssignedTo)
        {
            throw new NotSupportedException(
                "It's not supported to modify the content of a variable coming from the parent scope in a lambda " +
                "expression. Pass arguments instead. Affected method: " + Environment.NewLine + method);
        }
    }

    private sealed class MemberReferenceExpressionVisitingVisitor : DepthFirstAstVisitor
    {
        private readonly Action<MemberReferenceExpression> _expressionProcessor;

        public MemberReferenceExpressionVisitingVisitor(Action<MemberReferenceExpression> expressionProcessor) =>
            _expressionProcessor = expressionProcessor;

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);
            _expressionProcessor(memberReferenceExpression);
        }
    }
}
