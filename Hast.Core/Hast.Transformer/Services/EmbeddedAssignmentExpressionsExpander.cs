using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

/// <summary>
/// Searches for assignment expressions embedded in other expressions and brings them up to their own statements,
/// allowing easier processing later.
/// </summary>
/// <example>
/// <code>
/// if (skipCount = skipCount - 1u &lt;= 0u)
/// {
///     ...
///
/// ...will be converted into:
/// uint assignment;
/// assignment = skipCount - 1u;
/// skipCount = assignment;
/// if (assignment &lt;= 0u)
/// {
///     ...
/// </code>
/// </example>
/// <remarks>
/// <para>
/// The MakeAssignmentExpressions configuration of <see cref="ICSharpCode.Decompiler.DecompilerSettings"/> serves
/// something similar but that also changes how a decompiled Task.Factory.StartNew() looks like.
/// </para>
/// </remarks>
public class EmbeddedAssignmentExpressionsExpander : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ObjectInitializerExpander) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new EmbeddedAssignmentExpressionsExpandingVisitor());

    private sealed class EmbeddedAssignmentExpressionsExpandingVisitor : DepthFirstAstVisitor
    {
        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            var type = assignmentExpression.GetActualType();

            if (assignmentExpression.Parent is Statement ||
                assignmentExpression.Parent is Attribute ||
                // This is a DisplayClass-related if, those are handled specially later on.
                type.IsFunc())
            {
                return;
            }

            // Saving the right side of the assignment to a variable and then using that instead of the original
            // embedded assignment. Not using the left side directly later because that can be any complex value access,
            // keeping it simple.
            var variableIdentifier = VariableHelper.DeclareAndReferenceVariable(
                "assignment",
                assignmentExpression,
                TypeHelper.CreateAstType(type));

            var firstParentStatement = assignmentExpression.FindFirstParentStatement();
            var resolveResult = assignmentExpression.CreateResolveResultFromActualType();

            var tempVariableAssignment = new AssignmentExpression(variableIdentifier, assignmentExpression.Right.Clone())
                .WithAnnotation(resolveResult);

            AstInsertionHelper.InsertStatementBefore(firstParentStatement, new ExpressionStatement(tempVariableAssignment));

            var leftAssignment = new AssignmentExpression(assignmentExpression.Left.Clone(), variableIdentifier.Clone())
                .WithAnnotation(resolveResult);

            AstInsertionHelper.InsertStatementBefore(firstParentStatement, new ExpressionStatement(leftAssignment));

            assignmentExpression.ReplaceWith(variableIdentifier.Clone());
        }
    }
}
