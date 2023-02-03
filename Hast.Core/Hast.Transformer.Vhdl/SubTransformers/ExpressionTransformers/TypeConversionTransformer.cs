using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

public class TypeConversionTransformer : ITypeConversionTransformer
{
    private readonly ITypeConverter _typeConverter;

    public TypeConversionTransformer(ITypeConverter typeConverter) => _typeConverter = typeConverter;

    public IVhdlElement ImplementTypeConversionForBinaryExpression(
        BinaryOperatorExpression binaryOperatorExpression,
        DataObjectReference variableReference,
        bool isLeft,
        SubTransformerContext context)
    {
        // If this is some null check then no need for any type conversion.
        if (binaryOperatorExpression.EitherIs<NullReferenceExpression>())
        {
            return variableReference;
        }

        // If the type of an operand can't be determined the best guess is the expression's type.
        var expressionType = binaryOperatorExpression.GetActualType();
        var expressionVhdlType = expressionType != null ?
            _typeConverter.ConvertType(expressionType, context.TransformationContext) :
            null;

        var (leftType, rightType) = GetLeftAndRightTypes(binaryOperatorExpression, expressionType);

        var leftVhdlType = leftType != null ?
            _typeConverter.ConvertType(leftType, context.TransformationContext) :
            expressionVhdlType;

        var rightVhdlType = rightType != null ?
            _typeConverter.ConvertType(rightType, context.TransformationContext) :
            expressionVhdlType;

        if ((leftVhdlType ?? rightVhdlType) == null)
        {
            throw new InvalidOperationException(
                "The type of the operands of the following expression couldn't be determined: " +
                binaryOperatorExpression.ToString().AddParentEntityName(binaryOperatorExpression));
        }

        if (leftVhdlType == rightVhdlType) return variableReference;

        var convertToLeftType =
            expressionType?.Equals(leftType) == true ||
            expressionType?.Equals(rightType) == true
                // Is the result type of the expression equal to one of the operands? Then convert the other operand.
                ? expressionType.Equals(leftType)
                // If the result type of the expression is something else (e.g. if the operation is inequality then for
                // two integer operands the result type will be boolean) then convert in a way that's lossless.
                : ImplementTypeConversion(leftVhdlType, rightVhdlType, Empty.Instance).IsLossy;

        var fromType = convertToLeftType ? rightVhdlType : leftVhdlType;
        var toType = convertToLeftType ? leftVhdlType : rightVhdlType;

        if ((isLeft && toType == leftVhdlType) || (!isLeft && toType == rightVhdlType))
        {
            return variableReference;
        }

        var typeConversionResult = ImplementTypeConversion(fromType, toType, variableReference);
        if (typeConversionResult.IsLossy)
        {
            context.Scope.Warnings.AddWarning(
                "LossyBinaryExpressionCast",
                $"Converting from {fromType?.Name} to {toType?.Name} to fix a binary expression. Although valid in" +
                $" .NET this could cause information loss due to rounding. The affected expression is " +
                $"{binaryOperatorExpression} in member {binaryOperatorExpression.FindFirstParentOfType<EntityDeclaration>().GetFullName()}.");
        }

        return typeConversionResult.ConvertedFromExpression;
    }

    public IAssignmentTypeConversionResult ImplementTypeConversionForAssignment(
        DataType fromType,
        DataType toType,
        IVhdlElement fromExpression,
        IDataObject toDataObject)
    {
        var subResult = ImplementTypeConversion(fromType, toType, fromExpression);

        var result = new AssignmentTypeConversionResult
        {
            ConvertedFromExpression = subResult.ConvertedFromExpression,
            ConvertedToDataObject = toDataObject,
            IsLossy = subResult.IsLossy,
            IsResized = subResult.IsResized,
        };

        // If both types are arrays then if the array size is different slicing is needed.
        var fromArray = fromType as UnconstrainedArrayInstantiation;
        var toArray = toType as UnconstrainedArrayInstantiation;
        if (fromArray != null && toArray != null && fromArray.RangeTo < toArray.RangeTo)
        {
            result.ConvertedToDataObject = new ArraySlice
            {
                ArrayReference = toDataObject,
                IndexFrom = 0,
                IndexTo = fromArray.RangeTo,
            };
            result.IsResized = true;
        }

        return result;
    }

    public ITypeConversionResult ImplementTypeConversion(DataType fromType, DataType toType, IVhdlElement fromExpression)
    {
        if (fromType == toType)
        {
            return ImplementTypeConversionWithMatchingTypes(
                fromType,
                toType,
                fromExpression);
        }

        var result = new TypeConversionResult();
        var fromSize = fromType.GetSize();
        var toSize = toType.GetSize();

        Invocation CreateCastInvocationForFromExpression(string target) => new(target, fromExpression);

        // Trying supported cast scenarios:
        bool shouldReturn = TrySupportedCastScenarios(fromType, fromSize, fromExpression, toType, toSize, result);
        if (shouldReturn) return result;

        if (fromType == KnownDataTypes.StdLogicVector32)
        {
            if (KnownDataTypes.SignedIntegers.Contains(toType))
            {
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("signed");
            }
            else if (KnownDataTypes.UnsignedIntegers.Contains(toType))
            {
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("unsigned");
            }

            result.IsLossy = toSize > 32;
        }

        if (toType == KnownDataTypes.StdLogicVector32)
        {
            result.ConvertedFromExpression = CreateCastInvocationForFromExpression("std_logic_vector");
            result.IsLossy = fromSize > 32;
        }

        if (result.ConvertedFromExpression == null)
        {
            throw new NotSupportedException(
                "Casting from " + fromType.Name + " to " + toType.Name +
                " is not supported. Transformed expression to be cast: " + fromExpression.ToVhdl());
        }

        return result;
    }

    private static bool TrySupportedCastScenarios(
        DataType fromType,
        int fromSize,
        IVhdlElement fromExpression,
        DataType toType,
        int toSize,
        TypeConversionResult result)
    {
        Invocation CreateCastInvocationForFromExpression(string target) => new(target, fromExpression);
        IVhdlElement CreateResizeExpression(IVhdlElement parameter)
        {
            result.IsResized = true;

            // There needs to be some decision logic on size in SmartResize() because sometimes the sizes in VHDL won't
            // be the same as in .NET due to type handling.
            return ResizeHelper.SmartResize(parameter, toSize);
        }

        var state = new
        {
            FromSigned = KnownDataTypes.SignedIntegers.Contains(fromType),
            ToSigned = KnownDataTypes.SignedIntegers.Contains(toType),
            FromUnsigned = KnownDataTypes.UnsignedIntegers.Contains(fromType),
            ToUnsigned = KnownDataTypes.UnsignedIntegers.Contains(toType),
            FromInteger = KnownDataTypes.Integers.Contains(fromType),
            ToInteger = KnownDataTypes.Integers.Contains(toType),
            FromReal = fromType == KnownDataTypes.Real,
            ToReal = toType == KnownDataTypes.Real,
            ToUnranged = toType == KnownDataTypes.UnrangedInt,
        };

        switch (state)
        {
            case { FromSigned: true, ToSigned: true }:
            case { FromUnsigned: true, ToUnsigned: true }:
                if (fromSize == toSize)
                {
                    result.ConvertedFromExpression = fromExpression;
                    return true;
                }

                // Casting to a smaller type, so we need to cut off bits. Casting to a bigger type is not lossy but
                // still needs resize.
                if (fromSize > toSize)
                {
                    result.IsLossy = true;
                }

                result.ConvertedFromExpression = CreateResizeExpression(fromExpression);

                return false;
            case { FromInteger: true, ToReal: true }:
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("real");
                return false;
            case { FromReal: true, ToInteger: true }:
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("integer");
                return false;
            case { FromUnsigned: true, ToSigned: true }:
                // If the full scale of the uint wouldn't fit.
                result.IsLossy = fromSize > toSize / 2;

                var expression = fromExpression;

                // Resizing needs to happen before signed() otherwise casting an unsigned to signed can result in data
                // loss due to the range change.
                if (fromSize != toSize)
                {
                    expression = CreateResizeExpression(fromExpression);
                }

                result.ConvertedFromExpression = new Invocation("signed", expression);
                return false;
            case { FromSigned: true, ToUnsigned: true } when fromSize < toSize:
                result.IsLossy = true;

                var expandInvocation = CreateCastInvocationForFromExpression("ToUnsignedAndExpand");
                expandInvocation.Parameters.Add(toSize.ToVhdlValue(KnownDataTypes.UnrangedInt));
                result.ConvertedFromExpression = expandInvocation;
                result.IsResized = true;

                return false;
            case { FromSigned: true, ToUnsigned: true }:
                result.IsLossy = true;
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("unsigned");

                if (fromSize != toSize)
                {
                    result.ConvertedFromExpression = CreateResizeExpression(result.ConvertedFromExpression);
                }

                return false;
            case { FromInteger: true, ToUnranged: true }:
                result.IsLossy = true;
                result.ConvertedFromExpression = CreateCastInvocationForFromExpression("to_integer");
                return false;
            default:
                return false;
        }
    }

    private static ITypeConversionResult ImplementTypeConversionWithMatchingTypes(
        DataType fromType,
        DataType toType,
        IVhdlElement fromExpression)
    {
        var result = new TypeConversionResult();

        // If both types are arrays then if the array size is different slicing is needed.
        var fromArray = fromType as UnconstrainedArrayInstantiation;
        var toArray = toType as UnconstrainedArrayInstantiation;
        if (fromArray != null && toArray != null && fromArray.RangeTo > toArray.RangeTo)
        {
            result.ConvertedFromExpression = new ArraySlice
            {
                ArrayReference = (IDataObject)fromExpression,
                IndexFrom = 0,
                IndexTo = toArray.RangeTo,
            };
            result.IsResized = true;
        }
        else
        {
            result.ConvertedFromExpression = fromExpression;
        }

        return result;
    }

    private static (IType LeftType, IType RightType) GetLeftAndRightTypes(
        BinaryOperatorExpression binaryOperatorExpression,
        IType expressionType)
    {
        var leftType = binaryOperatorExpression.Left.GetActualType();
        var rightType = binaryOperatorExpression.Right.GetActualType();

        // We won't get a type reference if the expression is a PrimitiveExpression (a constant). In this case we'll
        // assume that the type of the two sides is the same.
        if (binaryOperatorExpression.EitherIs<PrimitiveExpression>())
        {
            //// If both of them are PrimitiveExpressions that's something strange (like writing e.g. "if (1 == 3) { ....").
            //// Let's assume that then the correct type is that of the expression's.
            if (binaryOperatorExpression.BothAre<PrimitiveExpression>())
            {
                leftType = expressionType;
            }
            else if (leftType == null && binaryOperatorExpression.Left is PrimitiveExpression)
            {
                leftType = rightType;
            }
            else
            {
                rightType = leftType;
            }
        }

        return (leftType, rightType);
    }

    private class TypeConversionResult : ITypeConversionResult
    {
        public IVhdlElement ConvertedFromExpression { get; set; }
        public bool IsLossy { get; set; }
        public bool IsResized { get; set; }
    }

    private sealed class AssignmentTypeConversionResult : TypeConversionResult, IAssignmentTypeConversionResult
    {
        public IDataObject ConvertedToDataObject { get; set; }
    }
}
