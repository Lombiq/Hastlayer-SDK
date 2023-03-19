using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Removes those variables from the syntax tree which are just aliases to another variable and thus unneeded. Due to
/// reference behavior such alias variables make hardware generation much more complicated so it's better to get rid of
/// them.
/// </summary>
/// <example>
/// <code>
/// internal KpzKernelsTaskState &lt;ScheduleIterations&gt;b__9_0 (KpzKernelsTaskState rawTaskState)
/// {
///     KpzKernelsTaskState kpzKernelsTaskState;
///     kpzKernelsTaskState = rawTaskState;
///     // kpzKernelsTaskState is being used from now on everywhere so better to just use rawTaskState directly.
///     return kpzKernelsTaskState;
/// }
///
/// // The variable "random" is unneeded here.
/// RandomMwc64X random;
/// random = array [num4].Random1;
/// random.State = (random.State | ((ulong)num8 &lt;&lt; 32));
/// </code>
/// </example>
public class UnneededReferenceVariablesRemover : IConverter
{
    // Many other dependencies are just leftovers from the previous linear execution order. However in this case we know
    // explicitly that RefLocalVariablesRemover must come before UnneededReferenceVariablesRemover.
    public virtual IEnumerable<string> Dependencies { get; } = new[] { nameof(RefLocalVariablesRemover) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new AssignmentsDiscoveringVisitor());

    private sealed class AssignmentsDiscoveringVisitor : DepthFirstAstVisitor
    {
        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            var left = assignmentExpression.Left;
            var right = assignmentExpression.Right;

            // Let's check whether the assignment is for a reference type and whether it's between two variables or a
            // variable and a field/property/array item access (properties at these stage are only auto-properties,
            // custom properties are already converted to methods).
            if (assignmentExpression.IsPotentialAliasAssignment())
            {
                var parentEntity = assignmentExpression.FindFirstParentEntityDeclaration();
                var leftIdentifierExpression = (IdentifierExpression)left;

                // Now let's check if the left side is only ever assigned to once.
                var assignmentsCheckingVisitor = new AssignmentsCheckingVisitor(leftIdentifierExpression.Identifier);
                parentEntity.AcceptVisitor(assignmentsCheckingVisitor);

                if (assignmentsCheckingVisitor.AssignedToOnce == true)
                {
                    parentEntity.AcceptVisitor(new IdentifiersChangingVisitor(
                        leftIdentifierExpression.Identifier,
                        right));

                    parentEntity
                        .FindFirstChildOfType<VariableDeclarationStatement>(variableDeclaration =>
                            variableDeclaration.Variables.SingleOrDefault()?.Name == leftIdentifierExpression.Identifier)
                        ?.Remove();
                    assignmentExpression.FindFirstParentStatement().Remove();
                }
            }
        }
    }

    private sealed class AssignmentsCheckingVisitor : DepthFirstAstVisitor
    {
        private readonly string _identifier;

        public bool? AssignedToOnce { get; private set; }

        public AssignmentsCheckingVisitor(string identifier) => _identifier = identifier;

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            base.VisitAssignmentExpression(assignmentExpression);

            if (assignmentExpression.Left is IdentifierExpression identifierExpression &&
                identifierExpression.Identifier == _identifier)
            {
                AssignedToOnce = AssignedToOnce == null;
            }
        }
    }

    private sealed class IdentifiersChangingVisitor : DepthFirstAstVisitor
    {
        private readonly string _oldIdentifier;
        private readonly Expression _newExpression;

        public IdentifiersChangingVisitor(string oldIdentifier, Expression newExpression)
        {
            _oldIdentifier = oldIdentifier;
            _newExpression = newExpression;
        }

        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            if (identifierExpression.Identifier != _oldIdentifier) return;

            identifierExpression.ReplaceWith(_newExpression.Clone());
        }
    }
}
