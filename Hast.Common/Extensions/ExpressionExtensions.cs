using System.Reflection;

namespace System.Linq.Expressions;

public static class ExpressionExtensions
{
    /// <summary>
    /// Gets the full name (namespace, type name, method name, return type and argument types) of an instance method
    /// called in an expression.
    /// </summary>
    /// <typeparam name="T">The type of the method's object.</typeparam>
    /// <param name="expression">An expression with a call to the method.</param>
    public static string GetMethodFullName<T>(this Expression<Action<T>> expression) =>
        expression.GetInvokedMethodInfo().GetFullName();

    /// <summary>
    /// Gets the full name (namespace, type name, method name) of an instance method called in an expression.
    /// </summary>
    /// <typeparam name="T">The type of the method's object.</typeparam>
    /// <param name="expression">An expression with a call to the method.</param>
    public static string GetMethodSimpleName<T>(this Expression<Action<T>> expression)
    {
        var methodInfo = expression.GetInvokedMethodInfo();
        return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
    }

    private static MethodInfo GetInvokedMethodInfo<T>(this Expression<Action<T>> expression)
    {
        // Since we need a specific method (i.e. if there are multiple overloads of it, then still one of them)
        // this can't be simpler than requiring an expression.

        if (expression.Body is not MethodCallExpression methodCallExpression)
        {
            throw new InvalidOperationException("The supplied expression is not a method call.");
        }

        return methodCallExpression.Method;
    }
}
