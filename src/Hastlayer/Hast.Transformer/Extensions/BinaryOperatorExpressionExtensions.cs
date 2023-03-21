using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class BinaryOperatorExpressionExtensions
{
    public static IType GetResultType(this BinaryOperatorExpression expression)
    {
        var resultType = expression.GetActualType();
        resultType ??= expression.FindFirstNonParenthesizedExpressionParent().GetActualType();

        return resultType;
    }
}
