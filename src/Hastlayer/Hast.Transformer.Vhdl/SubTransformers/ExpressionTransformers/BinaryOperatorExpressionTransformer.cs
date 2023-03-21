using Hast.Synthesis;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

public class BinaryOperatorExpressionTransformer : IBinaryOperatorExpressionTransformer
{
    private readonly ITypeConverter _typeConverter;
    private readonly ITypeConversionTransformer _typeConversionTransformer;
    private readonly IEnumerable<IBinaryOperatorExpressionTransformerMultiCycleHandler> _multiCycleHandlers;

    public BinaryOperatorExpressionTransformer(
        ITypeConverter typeConverter,
        ITypeConversionTransformer typeConversionTransformer,
        IEnumerable<IBinaryOperatorExpressionTransformerMultiCycleHandler> multiCycleHandlers)
    {
        _typeConverter = typeConverter;
        _typeConversionTransformer = typeConversionTransformer;
        _multiCycleHandlers = multiCycleHandlers;
    }

    public IEnumerable<IVhdlElement> TransformParallelBinaryOperatorExpressions(
          IEnumerable<PartiallyTransformedBinaryOperatorExpression> partiallyTransformedExpressions,
          SubTransformerContext context)
    {
        var resultReferences = new List<IVhdlElement>();

        var partiallyTransformedExpressionsList = partiallyTransformedExpressions.ToList();

        resultReferences.Add(TransformBinaryOperatorExpressionInner(
            partiallyTransformedExpressionsList[0],
            operationResultDataObjectIsVariable: false,
            isFirstOfSimdOperationsOrIsSingleOperation: true,
            isLastOfSimdOperations: false,
            context));

        for (int i = 1; i < partiallyTransformedExpressionsList.Count - 1; i++)
        {
            resultReferences.Add(TransformBinaryOperatorExpressionInner(
                partiallyTransformedExpressionsList[i],
                operationResultDataObjectIsVariable: false,
                isFirstOfSimdOperationsOrIsSingleOperation: false,
                isLastOfSimdOperations: false,
                context));
        }

        resultReferences.Add(TransformBinaryOperatorExpressionInner(
            partiallyTransformedExpressionsList[^1],
            operationResultDataObjectIsVariable: false,
            isFirstOfSimdOperationsOrIsSingleOperation: false,
            isLastOfSimdOperations: true,
            context));

        return resultReferences;
    }

    public IVhdlElement TransformBinaryOperatorExpression(
        PartiallyTransformedBinaryOperatorExpression partiallyTransformedExpression,
        SubTransformerContext context) =>
        TransformBinaryOperatorExpressionInner(
            partiallyTransformedExpression,
            operationResultDataObjectIsVariable: true,
            isFirstOfSimdOperationsOrIsSingleOperation: true,
            isLastOfSimdOperations: true,
            context);

    private IVhdlElement TransformBinaryOperatorExpressionInner(
        PartiallyTransformedBinaryOperatorExpression partiallyTransformedExpression,
        bool operationResultDataObjectIsVariable,
        bool isFirstOfSimdOperationsOrIsSingleOperation,
        bool isLastOfSimdOperations,
        SubTransformerContext context)
    {
        var binary = new Binary
        {
            Left = partiallyTransformedExpression.LeftTransformed,
            Right = partiallyTransformedExpression.RightTransformed,
        };

        var expression = partiallyTransformedExpression.BinaryOperatorExpression;

        var leftType = expression.Left.GetActualType();
        var rightType = expression.Right.GetActualType();

        ThrowIfUnsupported(leftType, rightType, expression);

        binary.Operator = expression.Operator switch
        {
            BinaryOperatorType.Add => BinaryOperator.Add,
            BinaryOperatorType.BitwiseAnd or BinaryOperatorType.ConditionalAnd => BinaryOperator.And,
            BinaryOperatorType.BitwiseOr or BinaryOperatorType.ConditionalOr => BinaryOperator.Or,
            BinaryOperatorType.Divide => BinaryOperator.Divide,
            BinaryOperatorType.Equality => BinaryOperator.Equality,
            BinaryOperatorType.ExclusiveOr => BinaryOperator.ExclusiveOr,
            BinaryOperatorType.GreaterThan => BinaryOperator.GreaterThan,
            BinaryOperatorType.GreaterThanOrEqual => BinaryOperator.GreaterThanOrEqual,
            BinaryOperatorType.InEquality => BinaryOperator.InEquality,
            BinaryOperatorType.LessThan => BinaryOperator.LessThan,
            BinaryOperatorType.LessThanOrEqual => BinaryOperator.LessThanOrEqual,
            // The % operator in .NET, called modulus in the AST, is in reality a different remainder operator.
            BinaryOperatorType.Modulus => BinaryOperator.Remainder,
            BinaryOperatorType.Multiply => BinaryOperator.Multiply,
            // Left and right shift for numerical types is a function call in VHDL, so handled separately. See below.
            // The sll/srl or sra/sla operators shouldn't be used, see:
            // https://www.nandland.com/vhdl/examples/example-shifts.html and
            // https://stackoverflow.com/questions/9018087/shift-a-std-logic-vector-of-n-bit-to-right-or-left
            BinaryOperatorType.ShiftLeft or BinaryOperatorType.ShiftRight => binary.Operator,
            BinaryOperatorType.Subtract => BinaryOperator.Subtract,
            _ => throw new NotSupportedException("Binary operator " + expression.Operator + " is not supported."),
        };
        var stateMachine = context.Scope.StateMachine;
        var currentBlock = context.Scope.CurrentBlock;

        var firstNonParenthesizedExpressionParent = expression.FindFirstNonParenthesizedExpressionParent();
        var resultType = expression.GetResultType();
        var isMultiplication = expression.Operator == BinaryOperatorType.Multiply;

        IType preCastType = null;
        // If the parent is an explicit cast then we need to follow that, otherwise there could be a resize to a smaller
        // type here, then a resize to a bigger type as a result of the cast.
        var hasExplicitCast = firstNonParenthesizedExpressionParent is CastExpression;
        if (hasExplicitCast)
        {
            preCastType = resultType;
            resultType = firstNonParenthesizedExpressionParent.GetActualType();
        }

        var resultVhdlType = _typeConverter.ConvertType(resultType, context.TransformationContext);
        var resultTypeSize = resultVhdlType.GetSize();

        var operationResultDataObjectReference = operationResultDataObjectIsVariable
            ? stateMachine
                .CreateVariableWithNextUnusedIndexedName("binaryOperationResult", resultVhdlType)
                .ToReference()
            : stateMachine
                .CreateSignalWithNextUnusedIndexedName("binaryOperationResult", resultVhdlType)
                .ToReference();

        IVhdlElement binaryElement = binary;

        var leftTypeInfo = GetTypeAndSize(leftType, context, allowNullKind: true);
        var rightTypeInfo = GetTypeAndSize(rightType, context, allowNullKind: false);

        var shouldResizeResult = ShouldResize(
            isMultiplication,
            leftTypeInfo,
            rightTypeInfo,
            resultType,
            resultTypeSize,
            expression);

        var maxOperandSize = Math.Max(leftTypeInfo.Size, rightTypeInfo.Size);
        if (maxOperandSize == 0) maxOperandSize = resultTypeSize;

        var deviceDriver = context.TransformationContext.DeviceDriver;
        var clockCyclesNeededForSignedOperation = deviceDriver
            .GetClockCyclesNeededForBinaryOperation(expression, maxOperandSize, isSigned: true);
        var clockCyclesNeededForUnsignedOperation = deviceDriver
            .GetClockCyclesNeededForBinaryOperation(expression, maxOperandSize, isSigned: false);

        var clockCyclesNeededForOperation = (leftTypeInfo.DataType?.Name, rightTypeInfo.DataType?.Name) switch
        {
            ("signed", "signed") => clockCyclesNeededForSignedOperation,
            ({ } left, { } right) when left == right => clockCyclesNeededForUnsignedOperation,
            // If the operands have different signs then let's take the slower version just to be safe.
            _ => Math.Max(clockCyclesNeededForSignedOperation, clockCyclesNeededForUnsignedOperation),
        };

        var handleShiftResult = HandleShift(
            context,
            expression,
            binary,
            binaryElement,
            deviceDriver,
            leftTypeInfo,
            maxOperandSize,
            clockCyclesNeededForOperation,
            hasExplicitCast,
            preCastType,
            resultVhdlType,
            shouldResizeResult);
        shouldResizeResult = handleShiftResult.ShouldResizeResult;
        binaryElement = handleShiftResult.BinaryElement;
        clockCyclesNeededForOperation = handleShiftResult.ClockCyclesNeededForOperation;

        if (shouldResizeResult)
        {
            var invocation = new Invocation
            {
                Target = ResizeHelper.SmartResizeName.ToVhdlIdValue(),
            };
            invocation.Parameters.Add(binaryElement);
            invocation.Parameters.Add(resultTypeSize.ToVhdlValue(KnownDataTypes.UnrangedInt));
            binaryElement = invocation;
        }

        var operationResultAssignment = new Assignment
        {
            AssignTo = operationResultDataObjectReference,
            Expression = binaryElement,
        };

        var operationIsMultiCycle = clockCyclesNeededForOperation > 1;

        HandleMultiCycle(
            context,
            stateMachine,
            operationIsMultiCycle,
            operationResultDataObjectIsVariable,
            operationResultDataObjectReference);

        // If the current state already takes more than one clock cycle we add a new state and follow up there.
        if (isFirstOfSimdOperationsOrIsSingleOperation && !operationIsMultiCycle)
        {
            stateMachine.AddNewStateAndChangeCurrentBlockIfOverOneClockCycle(context, clockCyclesNeededForOperation);
        }

        // If the operation in itself doesn't take more than one clock cycle then we simply add the operation to the
        // current block, which can be in a new state added previously above.
        if (!operationIsMultiCycle)
        {
            HandleSingleCycleOperation(
                currentBlock,
                operationResultAssignment,
                isFirstOfSimdOperationsOrIsSingleOperation,
                clockCyclesNeededForOperation);

            return operationResultDataObjectReference;
        }

        // Since the operation in itself takes more than one clock cycle we need to add a new state just to wait. Then
        // we transition from that state forward to a state where the actual algorithm continues.
        var clockCyclesToWait = (int)Math.Ceiling(clockCyclesNeededForOperation);

        // Building the wait state, just when this is the first transform of multiple SIMD operations (or is a single
        // operation).
        BuildWaitState(
            isFirstOfSimdOperationsOrIsSingleOperation,
            context,
            stateMachine,
            operationResultDataObjectReference,
            clockCyclesToWait);

        currentBlock.Add(operationResultAssignment);
        stateMachine.RecordMultiCycleOperation(operationResultDataObjectReference, clockCyclesToWait);

        // Changing the current block to the one in the state after the wait state, just when this is the last transform
        // of multiple SIMD operations (or is a single operation).
        if (isLastOfSimdOperations)
        {
            // It should be the last state added above.
            currentBlock.ChangeBlockToDifferentState(stateMachine.States[^1].Body, stateMachine.States.Count - 1);
        }

        return operationResultDataObjectReference;
    }

    private void HandleMultiCycle(
        SubTransformerContext context,
        IMemberStateMachine stateMachine,
        bool operationIsMultiCycle,
        bool operationResultDataObjectIsVariable,
        IDataObject operationResultDataObjectReference)
    {
        if (!operationIsMultiCycle) return;

        foreach (var handler in _multiCycleHandlers)
        {
            handler.Handle(
                context,
                stateMachine,
                operationResultDataObjectIsVariable,
                operationResultDataObjectReference);
        }
    }

    private static void BuildWaitState(
        bool isFirstOfSimdOperationsOrIsSingleOperation,
        SubTransformerContext context,
        IMemberStateMachine stateMachine,
        IDataObject operationResultDataObjectReference,
        int clockCyclesToWait)
    {
        if (!isFirstOfSimdOperationsOrIsSingleOperation) return;

        var waitedCyclesCountVariable = stateMachine.CreateVariableWithNextUnusedIndexedName(
            "clockCyclesWaitedForBinaryOperationResult",
            KnownDataTypes.Int32);
        var waitedCyclesCountInitialValue = "0".ToVhdlValue(waitedCyclesCountVariable.DataType);
        waitedCyclesCountVariable.InitialValue = waitedCyclesCountInitialValue;
        var waitedCyclesCountVariableReference = waitedCyclesCountVariable.ToReference();

        var waitForResultBlock = new InlineBlock(
            new GeneratedComment(vhdlGenerationOptions =>
            {
                var vhdl = operationResultDataObjectReference.ToVhdl(vhdlGenerationOptions);
                return FormattableString.Invariant(
                    $"Waiting for the result to appear in {vhdl} (have to wait {clockCyclesToWait} clock cycles in this state).");
            }),
            new LineComment(
                "The assignment needs to be kept up for multi-cycle operations for the result to actually appear in the target."));

        var waitForResultIf = new IfElse
        {
            Condition = new Binary
            {
                Left = waitedCyclesCountVariableReference,
                Operator = BinaryOperator.GreaterThanOrEqual,
                Right = clockCyclesToWait.ToVhdlValue(waitedCyclesCountVariable.DataType),
            },
        };
        waitForResultBlock.Add(waitForResultIf);

        var waitForResultStateIndex = stateMachine.AddNewStateAndChangeCurrentBlock(context, waitForResultBlock);
        stateMachine.States[waitForResultStateIndex].RequiredClockCycles = clockCyclesToWait;

        var afterResultReceivedBlock = new InlineBlock();
        var afterResultReceivedStateIndex = stateMachine.AddState(afterResultReceivedBlock);
        waitForResultIf.True = new InlineBlock(
            stateMachine.CreateStateChange(afterResultReceivedStateIndex),
            new Assignment { AssignTo = waitedCyclesCountVariableReference, Expression = waitedCyclesCountInitialValue });
        waitForResultIf.Else = new Assignment
        {
            AssignTo = waitedCyclesCountVariableReference,
            Expression = new Binary
            {
                Left = waitedCyclesCountVariableReference,
                Operator = BinaryOperator.Add,
                Right = "1".ToVhdlValue(waitedCyclesCountVariable.DataType),
            },
        };
    }

    private VhdlTypeInfo GetTypeAndSize(IType type, SubTransformerContext context, bool allowNullKind)
    {
        DataType vhdlType = null;
        var typeSize = 0;

        if (type != null && (allowNullKind || type.Kind != TypeKind.Null))
        {
            vhdlType = _typeConverter.ConvertType(type, context.TransformationContext);
            typeSize = vhdlType.GetSize();
        }

        return new VhdlTypeInfo
        {
            Type = type,
            Size = typeSize,
            DataType = vhdlType,
        };
    }

    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "We can make an exception here to avoid illogically segmented ravioli code.")]
    private (bool ShouldResizeResult, IVhdlElement BinaryElement, decimal ClockCyclesNeededForOperation) HandleShift(
        SubTransformerContext context,
        BinaryOperatorExpression expression,
        Binary binary,
        IVhdlElement binaryElement,
        IDeviceDriver deviceDriver,
        VhdlTypeInfo leftTypeInfo,
        int maxOperandSize,
        decimal clockCyclesNeededForOperation,
        bool hasExplicitCast,
        IType preCastType,
        DataType resultVhdlType,
        bool shouldResizeResult)
    {
        var isShift = false;
        var newBinaryElement = binaryElement;

        if (expression.Operator is BinaryOperatorType.ShiftLeft or BinaryOperatorType.ShiftRight)
        {
            isShift = true;

            // Contrary to what happens in VHDL, binary shifting in .NET will only use the lower 5 bits (for 32b
            // operands) or 6 bits (for 64b operands) of the shift count. So e.g. 1 << 33 won't produce 0 (by shifting
            // out to the void) but 2, since only a shift by 1 happens (as 33 is 100001 in binary).
            // See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/left-shift-operator So
            // we need to truncate. Furthermore, both shifts will also do a bitwise AND with just 1s on the count, see:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/right-shift-operator How the
            // vacated bits are filled on shifting in either direction is the same (see:
            // https://www.csee.umbc.edu/portal/help/VHDL/numeric_std.vhdl).

            var countSize = leftTypeInfo.Size <= 32 ? 5 : 6;
            IVhdlElement resize = ResizeHelper.SmartResize(binary.Right, countSize);

            if (expression.Operator == BinaryOperatorType.ShiftRight)
            {
                // Since we're already resizing the additional "& 11111" (or "& 111111") might not be needed. However
                // it's just an identity operation due to the count parameter having the same size. Also, while this was
                // only added to right shifts .NET actually does the same for left shifts too. However, it seems to
                // work. Needs further testing to see if it can be removed (it was added in 21ae34098e48 without
                // anything else being changed and it did fix an issue).
                resize = new Binary
                {
                    Left = resize,
                    Operator = BinaryOperator.And,
                    Right =
                        string.Join(string.Empty, Enumerable.Repeat(1, countSize))
                            .ToVhdlValue(new StdLogicVector { SizeNumber = countSize }),
                };

                var bitwiseAndBinary = new BinaryOperatorExpression(
                    expression.Left.Clone(),
                    BinaryOperatorType.BitwiseAnd,
                    expression.Right.Clone());

                clockCyclesNeededForOperation += Math.Max(
                    deviceDriver.GetClockCyclesNeededForBinaryOperation(bitwiseAndBinary, maxOperandSize, isSigned: true),
                    deviceDriver.GetClockCyclesNeededForBinaryOperation(
                        bitwiseAndBinary.Clone<BinaryOperatorExpression>(), maxOperandSize, isSigned: false));
            }

            var invocation = new Invocation
            {
                Target = (expression.Operator == BinaryOperatorType.ShiftLeft ? "shift_left" : "shift_right")
                    .ToVhdlIdValue(),
            };
            invocation.Parameters.Add(binary.Left);
            // The result will be like to_integer(unsigned(SmartResize(..))). The cast to unsigned is necessary because
            // in .NET the input of the shift is always treated as unsigned. Right shifts will also have a bitwise AND
            // inside unsigned().
            invocation.Parameters.Add(Invocation.ToInteger(new Invocation("unsigned", resize)));

            newBinaryElement = invocation;
        }

        // Shifts also need type conversion if the right operator doesn't have the same type as the left one.
        if (hasExplicitCast || isShift)
        {
            var fromType = isShift && !hasExplicitCast ?
                leftTypeInfo.DataType :
                _typeConverter.ConvertType(preCastType, context.TransformationContext);

            var typeConversionResult = _typeConversionTransformer.ImplementTypeConversion(
                fromType,
                resultVhdlType,
                newBinaryElement);

            newBinaryElement = typeConversionResult.ConvertedFromExpression;

            // Most of the time due to the cast no resize is necessary, but sometimes it is.
            shouldResizeResult = shouldResizeResult && !typeConversionResult.IsResized;
        }

        return (shouldResizeResult, newBinaryElement, clockCyclesNeededForOperation);
    }

    private static bool ShouldResize(
        bool isMultiplication,
        VhdlTypeInfo left,
        VhdlTypeInfo right,
        IType resultType,
        int resultTypeSize,
        BinaryOperatorExpression expression)
    {
        var shouldResizeResult =
            //// If the type of the result is the same as the type of the binary expression but the expression is
            //// a multiplication then this means that the result of the operation wouldn't fit into the result
            //// type. This is allowed in .NET (an explicit cast is needed in C# but that will be removed by the
            //// compiler) but will fail in VHDL with something like "[Synth 8-690] width mismatch in assignment;
            //// target has 16 bits, source has 32 bits." In this cases we need to add a type conversion. Also
            //// see the block below.
            //// E.g. ushort = ushort * ushort is valid in IL but in VHDL it must have a length truncation:
            //// unsigned(15 downto 0) = resize(unsigned(15 downto 0) * unsigned(15 downto 0), 16)
            isMultiplication &&
            (
                resultType.Equals(expression.GetActualType()) ||
                (resultType.Equals(left.Type) && resultType.Equals(right.Type))
            );

        if (!shouldResizeResult && left.Type != null && right.Type != null)
        {
            shouldResizeResult =
                // If the operands and the result have the same size then the result won't fit.
                (isMultiplication &&
                     resultTypeSize != 0 &&
                     resultTypeSize == left.Size &&
                     resultTypeSize == right.Size)
                ||
                // If the operation is an addition and the types of the result and the operands differ then we also have
                // to resize.
                (expression.Operator == BinaryOperatorType.Add &&
                    !(resultType.Equals(left.Type) && resultType.Equals(right.Type)))
                ||
                // If the operand and result sizes don't match.
                (resultTypeSize != 0 && (resultTypeSize != left.Size || resultTypeSize != right.Size));
        }

        return shouldResizeResult;
    }

    private static void ThrowIfUnsupported(IType leftType, IType rightType, BinaryOperatorExpression expression)
    {
        // At this point if non-primitive types are checked for equality it could mean that they are custom types either
        // without the equality operator defined or they are custom value types and a ReferenceEquals() is attempted on
        // them which is wrong.
        if ((((!leftType.IsPrimitive() || leftType.GetKnownTypeCode() == KnownTypeCode.Object) && leftType.Kind != TypeKind.Enum) ||
             ((!rightType.IsPrimitive() || rightType.GetKnownTypeCode() == KnownTypeCode.Object) && rightType.Kind != TypeKind.Enum))
            &&
            !(expression.Left is NullReferenceExpression || expression.Right is NullReferenceExpression))
        {
            string message = $"Unsupported operator in the following binary operator expression: {expression}. " +
                             $"This could mean that you attempted to use an operator on custom types either without the " +
                             $"operator being defined for the type or they are custom value types and you mistakenly tried " +
                             $"to use ReferenceEquals() on them.";
            throw new InvalidOperationException(message.AddParentEntityName(expression));
        }
    }

    private static void HandleSingleCycleOperation(
        CurrentBlock currentBlock,
        IVhdlElement operationResultAssignment,
        bool isFirstOfSimdOperationsOrIsSingleOperation,
        decimal clockCyclesNeededForOperation)
    {
        currentBlock.Add(operationResultAssignment);
        if (isFirstOfSimdOperationsOrIsSingleOperation)
        {
            currentBlock.RequiredClockCycles += clockCyclesNeededForOperation;
        }
    }

    private sealed class VhdlTypeInfo
    {
        public IType Type { get; set; }
        public int Size { get; set; }
        public DataType DataType { get; set; }
    }
}
