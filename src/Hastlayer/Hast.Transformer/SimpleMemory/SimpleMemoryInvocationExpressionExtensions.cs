using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class SimpleMemoryInvocationExpressionExtensions
{
    /// <summary>
    /// Determines whether the invocation expression is a SimpleMemory object member invocation.
    /// </summary>
    public static bool IsSimpleMemoryInvocation(this InvocationExpression expression) =>
        expression.GetMemberResolveResult()?.TargetResult?.Type.IsSimpleMemory() == true;
}
