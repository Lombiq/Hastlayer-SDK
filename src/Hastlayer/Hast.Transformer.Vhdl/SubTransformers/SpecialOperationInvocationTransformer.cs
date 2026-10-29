using Hast.Common.Numerics;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class SpecialOperationInvocationTransformer : ISpecialOperationInvocationTransformer
{
    private readonly IBinaryOperatorExpressionTransformer _binaryOperatorExpressionTransformer;
    private readonly ITypeConverter _typeConverter;

    public SpecialOperationInvocationTransformer(
        IBinaryOperatorExpressionTransformer binaryOperatorExpressionTransformer,
        ITypeConverter typeConverter)
    {
        _binaryOperatorExpressionTransformer = binaryOperatorExpressionTransformer;
        _typeConverter = typeConverter;
    }

    public bool IsSupported(AstNode node) =>
        node is InvocationExpression invocationExpression &&
        TryGetSimdOperation(invocationExpression.GetTargetMemberFullName()) != null;

    public IVhdlElement TransformSpecialOperationInvocation(
        InvocationExpression expression,
        IEnumerable<IVhdlElement> transformedParameters,
        SubTransformerContext context)
    {
        var targetMethodName = expression.GetTargetMemberFullName();

        if (TryGetSimdOperation(targetMethodName) is not { } simdOperation)
        {
            throw new InvalidOperationException(
                $"The given {nameof(expression)} ({expression}) is not a special operation invocation.");
        }

        if (string.IsNullOrEmpty(simdOperation))
        {
            throw new NotSupportedException(
                "No transformer logic exists for the following special operation invocation: " + expression);
        }

        // Transforming the operation to parallel signal-using operations.

        // The last argument for SIMD operations is always the max degree of parallelism.
        var parameters = transformedParameters.AsList(); // Convert to IList to prevent multiple enumerations.
        var maxDegreeOfParallelism = ((Value)parameters[^1]).Content.ToTechnicalInt();

        var vector1 = (DataObjectReference)parameters[0];
        var vector2 = (DataObjectReference)parameters[1];
        var binaryOperations = new List<PartiallyTransformedBinaryOperatorExpression>();

        var simdBinaryOperator = simdOperation switch
        {
            nameof(SimdOperations.AddVectors) => BinaryOperatorType.Add,
            nameof(SimdOperations.SubtractVectors) => BinaryOperatorType.Subtract,
            nameof(SimdOperations.MultiplyVectors) => BinaryOperatorType.Multiply,
            nameof(SimdOperations.DivideVectors) => BinaryOperatorType.Divide,
            _ => throw new NotSupportedException($"The SIMD operation {simdOperation} is not supported."),
        };

        // The result type of each artificial BinaryOperatorExpression should be the same as the SIMD method call's
        // return array type's element type.
        var resultElementResolveResult = expression.GetActualType().GetElementType().ToResolveResult();
        var intType = context.TransformationContext.KnownTypeLookupTable.Lookup(KnownTypeCode.Int32);

        for (int i = 0; i < maxDegreeOfParallelism; i++)
        {
            PrimitiveExpression GetIndexer(int index) =>
                new PrimitiveExpression(index).WithAnnotation(intType.ToResolveResult());

            var arrayReferences = expression.Arguments.Take(2).Select(expression => expression.Clone()).ToList();

            var binaryOperatorExpression = new BinaryOperatorExpression(
                    new IndexerExpression(arrayReferences[0], GetIndexer(i)),
                    simdBinaryOperator,
                    new IndexerExpression(arrayReferences[1], GetIndexer(i)));

            binaryOperatorExpression.AddAnnotation(resultElementResolveResult);

            var indexValue = Value.UnrangedInt(i);

            binaryOperations.Add(new PartiallyTransformedBinaryOperatorExpression
            {
                BinaryOperatorExpression = binaryOperatorExpression,
                LeftTransformed = new ArrayElementAccess { ArrayReference = vector1, IndexExpression = indexValue },
                RightTransformed = new ArrayElementAccess { ArrayReference = vector2, IndexExpression = indexValue },
            });
        }

        var stateMachine = context.Scope.StateMachine;

        var resultReferences = _binaryOperatorExpressionTransformer
            .TransformParallelBinaryOperatorExpressions(binaryOperations, context);

        // If no new states were added, i.e. the operation wasn't a multi-cycle one with wait states, then we add a new
        // state here: this is needed because accessing the results (since they are assigned to signals) should always
        // happen in a separate state. A state may have been added already because the state before the SIMD operations
        // would otherwise go over a clock cycle, but in that case we still need to add another one, so the fact alone
        // that there is a new state is not enough; checking if this is an empty state instead.
        var currentBlock = context.Scope.CurrentBlock;
        if (stateMachine.States[currentBlock.StateMachineStateIndex].Body.Body.Any())
        {
            currentBlock.Add(new LineComment(
                "A SIMD operation's results should always be read out in the next clock cycle at earliest, so closing the current state."));
            stateMachine.AddNewStateAndChangeCurrentBlock(context);
        }

        // Returning the results as an array initialization value (i.e.: array := (result1, result2);)
        return new Value
        {
            DataType = _typeConverter.ConvertType(
                expression.GetActualType(),
                context.TransformationContext),
            EvaluatedContent = new InlineBlock(resultReferences),
        };
    }

    private static string TryGetSimdOperation(string targetMethodName)
    {
        var simdOperationsClassFullNamePrefix = typeof(SimdOperations).FullName + "::";
        var simdOperations = new[]
        {
            nameof(SimdOperations.AddVectors),
            nameof(SimdOperations.SubtractVectors),
            nameof(SimdOperations.MultiplyVectors),
            nameof(SimdOperations.DivideVectors),
        };

        for (int i = 0; i < simdOperations.Length; i++)
        {
            if (targetMethodName.Contains(simdOperationsClassFullNamePrefix + simdOperations[i], StringComparison.InvariantCulture))
            {
                return simdOperations[i];
            }
        }

        return null;
    }
}
