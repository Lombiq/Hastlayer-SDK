using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Get rid of unnecessary compiler-generated <see cref="System.Threading.Tasks.Task"/> arrays.
/// </summary>
/// <example>
/// <para>
/// If <see cref="System.Threading.Tasks.Task"/> objects are saved to an array sometimes the compiler generates another
/// array variable without any apparent use. This makes transforming needlessly more complicated. E.g.
/// </para>
/// <code>
/// Task&lt;bool&gt;[] array;
/// Task&lt;bool&gt;[] arg_95_0;
/// array = new Task&lt;bool&gt;[35];
/// while (j &lt; 35)
/// {
///     arg_95_0 = array;
///     arg_95_0[arg_95_1] = arg_90_0.StartNew&lt;bool&gt;(arg_90_1, j);
///     j = j + 1;
/// }
/// Task.WhenAll&lt;bool&gt;(array).Wait();
/// </code>
/// <para>
/// Note that while there was the variable named <c>array</c> the compiler created <c>arg_95_0</c> and used it instead,
/// but just inside the loop.
/// </para>
/// </example>
public class GeneratedTaskArraysInliner : IConverter
{
    private const string TaskStart = "System.Threading.Tasks.Task`1<";

    public IEnumerable<string> Dependencies { get; } = new[] { nameof(BinaryAndUnaryOperatorExpressionsCastAdjuster) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable)
    {
        var inlinableTaskArraysFindingVisitor = new InlinableTaskArraysFindingVisitor();
        syntaxTree.AcceptVisitor(inlinableTaskArraysFindingVisitor);
        syntaxTree.AcceptVisitor(new InlinableTaskArraysInliningVisitor(inlinableTaskArraysFindingVisitor.InlinableVariableMapping));
    }

    private static bool IsTask(Expression expression) =>
        expression.GetActualTypeFullName().StartsWithOrdinal(TaskStart);

    private sealed class InlinableTaskArraysFindingVisitor : DepthFirstAstVisitor
    {
        public Dictionary<string, string> InlinableVariableMapping { get; set; } = new Dictionary<string, string>();

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            // AssigmentExpression, Left = arg_*, IsTask(Right)

            var compilerGeneratedVariableName = string.Empty;
            if (assignmentExpression.Left.Is<IdentifierExpression>(identifier =>
                {
                    compilerGeneratedVariableName = identifier.Identifier;
                    return compilerGeneratedVariableName.StartsWithOrdinal("arg_");
                }) &&
                IsTask(assignmentExpression.Right))
            {
                InlinableVariableMapping[compilerGeneratedVariableName] =
                    ((IdentifierExpression)assignmentExpression.Right).Identifier;
            }
        }
    }

    private sealed class InlinableTaskArraysInliningVisitor : DepthFirstAstVisitor
    {
        private readonly Dictionary<string, string> _inlinableVariableMappings;

        public InlinableTaskArraysInliningVisitor(Dictionary<string, string> inlinableVariableMappings) =>
            _inlinableVariableMappings = inlinableVariableMappings;

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            if (IsInlinableVariableIdentifier(identifierExpression))
            {
                var parentAssignment = identifierExpression.FindFirstParentOfType<AssignmentExpression>();
                // If this is in an arg_9C_0 = array; kind of assignment, then remove the whole assignment's statement.
                if (parentAssignment != null &&
                    parentAssignment.Left == identifierExpression &&
                    IsTask(parentAssignment.Right))
                {
                    parentAssignment.FindFirstParentOfType<ExpressionStatement>().Remove();
                }
                else
                {
                    identifierExpression.Identifier = _inlinableVariableMappings[identifierExpression.Identifier];
                }
            }
        }

        public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            base.VisitVariableDeclarationStatement(variableDeclarationStatement);

            if (_inlinableVariableMappings.ContainsKey(variableDeclarationStatement.Variables.Single().Name))
            {
                variableDeclarationStatement.Remove();
            }
        }

        private bool IsInlinableVariableIdentifier(IdentifierExpression identifierExpression) =>
            _inlinableVariableMappings.ContainsKey(identifierExpression.Identifier);
    }
}
