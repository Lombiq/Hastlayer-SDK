using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

public class AstExpressionEvaluator : IAstExpressionEvaluator
{
    public dynamic EvaluateBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
    {
        if (binaryOperatorExpression.Left is not PrimitiveExpression)
        {
            throw new NotSupportedException(
                "Evaluating only binary operator expressions where both operands are primitive expressions are " +
                "supported. The left expression was: " + binaryOperatorExpression.Left + ".");
        }

        if (binaryOperatorExpression.Right is not PrimitiveExpression)
        {
            throw new NotSupportedException(
                "Evaluating only binary operator expressions where both operands are primitive expressions are " +
                "supported. The right expression was: " + binaryOperatorExpression.Right + ".");
        }

        dynamic leftValue = ((PrimitiveExpression)binaryOperatorExpression.Left).Value;
        dynamic rightValue = ((PrimitiveExpression)binaryOperatorExpression.Right).Value;

        return binaryOperatorExpression.Operator switch
        {
            BinaryOperatorType.BitwiseAnd => leftValue & rightValue,
            BinaryOperatorType.BitwiseOr => leftValue | rightValue,
            BinaryOperatorType.ConditionalAnd => leftValue && rightValue,
            BinaryOperatorType.ConditionalOr => leftValue || rightValue,
            BinaryOperatorType.ExclusiveOr => leftValue ^ rightValue,
            BinaryOperatorType.GreaterThan => leftValue > rightValue,
            BinaryOperatorType.GreaterThanOrEqual => leftValue >= rightValue,
            BinaryOperatorType.Equality => leftValue.Equals(rightValue),
            BinaryOperatorType.InEquality => !leftValue.Equals(rightValue),
            BinaryOperatorType.LessThan => leftValue < rightValue,
            BinaryOperatorType.LessThanOrEqual => leftValue <= rightValue,
            BinaryOperatorType.Add => leftValue + rightValue,
            BinaryOperatorType.Subtract => leftValue - rightValue,
            BinaryOperatorType.Multiply => leftValue * rightValue,
            BinaryOperatorType.Divide => leftValue / rightValue,
            BinaryOperatorType.Modulus => leftValue % rightValue,
            BinaryOperatorType.ShiftLeft => leftValue << rightValue,
            BinaryOperatorType.ShiftRight => leftValue >> rightValue,
            BinaryOperatorType.NullCoalescing => leftValue ?? rightValue,
            _ => throw new NotSupportedException(
                "Evaluating binary operator expressions with the operator " + binaryOperatorExpression.Operator +
                " is not supported. Affected expression: " + binaryOperatorExpression),
        };
    }

    public dynamic EvaluateCastExpression(CastExpression castExpression)
    {
        if (castExpression.Expression is not PrimitiveExpression)
        {
            throw new NotSupportedException(
                "Evaluating only cast expressions that target a primitive expression are supported. The targeted expression was: " +
                castExpression.Expression + ".");
        }

        var toType = castExpression.GetActualType();
        dynamic value = ((PrimitiveExpression)castExpression.Expression).Value;

        return toType.GetFullName() switch
        {
            "System.Boolean" => (bool)value,
            "System.Byte" => (byte)value,
            "System.Char" => (char)value,
            "System.Decimal" => (decimal)value,
            "System.Double" => (double)value,
            "System.Int16" => (short)value,
            "System.Int32" => (int)value,
            "System.Int64" => (long)value,
            "System.Object" => value,
            "System.SByte" => (sbyte)value,
            "System.String" => (string)value,
            "System.UInt16" => (ushort)value,
            "System.UInt32" => (uint)value,
            "System.UInt64" => (ulong)value,
            _ => throw new NotSupportedException(
                "Evaluating casting to " + toType.GetFullName() + " is not supported. Affected expression: " +
                castExpression.ToString().AddParentEntityName(castExpression)),
        };
    }

    public dynamic EvaluateUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
    {
        if (unaryOperatorExpression.Expression is not PrimitiveExpression)
        {
            throw new NotSupportedException(
                "Evaluating only unary expressions that target a primitive expression are supported. The targeted expression was: " +
                unaryOperatorExpression.Expression + ".");
        }

        dynamic value = ((PrimitiveExpression)unaryOperatorExpression.Expression).Value;

        return unaryOperatorExpression.Operator switch
        {
            UnaryOperatorType.Not => !value,
            UnaryOperatorType.BitNot => ~value,
            UnaryOperatorType.Minus => -value,
            UnaryOperatorType.Plus => +value,
            UnaryOperatorType.Increment => value + 1,
            UnaryOperatorType.Decrement => value - 1,
            UnaryOperatorType.PostIncrement => value + 1,
            UnaryOperatorType.PostDecrement => value - 1,
            _ => throw new NotSupportedException(
                $"Evaluating unary operator expressions with the operator {unaryOperatorExpression.Operator} is" +
                $" not supported. Affected expression: {unaryOperatorExpression.ToString().AddParentEntityName(unaryOperatorExpression)}"),
        };
    }
}
