using Hast.Common.Interfaces;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

/// <summary>
/// Evaluates operator expressions found in the syntax tree.
/// </summary>
public interface IAstExpressionEvaluator : IDependency
{
    /// <summary>
    /// Returns the result of a binary operation.
    /// </summary>
    dynamic EvaluateBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression);

    /// <summary>
    /// Returns the result of a cast operation.
    /// </summary>
    dynamic EvaluateCastExpression(CastExpression castExpression);

    /// <summary>
    /// Returns the result of a unary operation.
    /// </summary>
    dynamic EvaluateUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression);
}
