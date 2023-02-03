namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class ExpressionExtensions
{
    public static bool EitherIs<T>(this BinaryOperatorExpression expression) =>
        expression?.Left is T || expression?.Right is T;

    public static bool BothAre<T>(this BinaryOperatorExpression expression) =>
        expression?.Left is T && expression.Right is T;
}
