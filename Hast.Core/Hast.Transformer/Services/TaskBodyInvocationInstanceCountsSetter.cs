using Hast.Layer;
using Hast.Transformer.Abstractions.Configuration;
using Hast.Transformer.Models;
using Hast.Transformer.Services.ConstantValuesSubstitution;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Transformer.Services;

/// <summary>
/// Configures the invocation instance count for the body delegates of <see cref="Task"/> s, i.e. determines how many
/// hardware copies a Task's body needs. Sets the <see cref="MemberInvocationInstanceCountConfiguration"/>
/// automatically.
/// </summary>
/// <example>
/// <para>For example in this case:</para>
/// <code>
/// for (uint i = 0; i &lt; 10; i++)
/// {
///     tasks[i] = Task.Factory.StartNew(
///         indexObject =&gt;
///         {
///             ...
///         },
///         i);
/// }
/// </code>
/// <para>...this service will be able to determine that the level of parallelism is 10.</para>
/// </example>
public class TaskBodyInvocationInstanceCountsSetter : IConverter
{
    // Many other dependencies are just leftovers from the previous linear execution order. However in this case we know
    // explicitly that ConstantValuesSubstitutor must come before TaskBodyInvocationInstanceCountsSetter.
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(ConstantValuesSubstitutor) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new TaskBodyInvocationInstanceCountsSetterVisitor(configuration.TransformerConfiguration()));

    private sealed class TaskBodyInvocationInstanceCountsSetterVisitor : DepthFirstAstVisitor
    {
        private readonly Dictionary<string, int> _taskStartsCountInMembers = new();
        private readonly TransformerConfiguration _configuration;

        public TaskBodyInvocationInstanceCountsSetterVisitor(TransformerConfiguration configuration) => _configuration = configuration;

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);

            if (!memberReferenceExpression.IsTaskStartNew()) return;

            var parentEntity = memberReferenceExpression.FindFirstParentEntityDeclaration();
            var parentEntityName = parentEntity.GetFullName();

            _taskStartsCountInMembers.TryGetValue(parentEntityName, out int taskStartsCountInMember);
            _taskStartsCountInMembers[parentEntityName] = taskStartsCountInMember + 1;

            var invokingMemberMaxInvocationConfiguration = _configuration
                .GetMaxInvocationInstanceCountConfigurationForMember(MemberInvocationInstanceCountConfiguration
                    .AddLambdaExpressionIndexToSimpleName(parentEntity.GetSimpleName(), taskStartsCountInMember));

            // Only do something if there's no invocation instance count configured.
            if (invokingMemberMaxInvocationConfiguration.MaxInvocationInstanceCount != 1) return;

            // Searching for a parent while statement that has a condition with a variable and a primitive expression,
            // i.e. something like num < 10.

            var parentWhile = memberReferenceExpression.FindFirstParentOfType<WhileStatement>();

            if (parentWhile == null ||
                !parentWhile.Condition.Is<BinaryOperatorExpression>(
                    expression =>
                        expression.Left is IdentifierExpression ||
                        expression.Left.FindFirstChildOfType<IdentifierExpression>() != null ||
                        expression.Right is IdentifierExpression ||
                        expression.Right.FindFirstChildOfType<IdentifierExpression>() != null,
                    out var condition))
            {
                return;
            }

            var primitiveExpression = condition.Left as PrimitiveExpression ?? condition.Left.FindFirstChildOfType<PrimitiveExpression>();
            if (primitiveExpression == null)
            {
                primitiveExpression = condition.Right as PrimitiveExpression ?? condition.Right.FindFirstChildOfType<PrimitiveExpression>();

                if (condition.Right.Is<BinaryOperatorExpression>(out var innerCondition))
                {
                    // In code decopmiled from F# it can happen that the expression will be decompiled into "1 + actual
                    // number"... Taking care of that here.
                    primitiveExpression = innerCondition.Right as PrimitiveExpression ??
                        innerCondition.Right.FindFirstChildOfType<PrimitiveExpression>();
                }
            }

            if (primitiveExpression == null) return;

            var valueString = primitiveExpression.Value.ToString();

            if (!int.TryParse(valueString, out var value)) return;

            if (condition.Operator == BinaryOperatorType.LessThan)
            {
                invokingMemberMaxInvocationConfiguration.MaxDegreeOfParallelism = value;
            }
            else if (condition.Operator == BinaryOperatorType.LessThanOrEqual)
            {
                invokingMemberMaxInvocationConfiguration.MaxDegreeOfParallelism = value - 1;
            }
        }
    }
}
