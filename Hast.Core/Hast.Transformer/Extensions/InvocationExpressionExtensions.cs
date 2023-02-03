using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class InvocationExpressionExtensions
{
    public static string GetTargetMemberFullName(this InvocationExpression expression) =>
        expression.GetReferencedMemberFullName();

    /// <summary>
    /// <para>Checks whether the invocation is a Task.Factory.StartNew call like either of the following.</para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>Task.Factory.StartNew(&lt;&gt;c__DisplayClass4_.&lt;&gt;9__0 ?? (&lt;&gt;c__DisplayClass4_.&lt;&gt;9__0 =
    /// &lt;&gt;c__DisplayClass4_.&lt;NameOfTaskStartingMethod&gt;b__0), inputArgument);</c>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>Task.Factory.StartNew((Func&lt;object, OutputType&gt;)this.&lt;NameOfTaskStartingMethod&gt;b__6_0,
    /// (object)inputArgument);</c>
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    public static bool IsTaskStart(this InvocationExpression expression) =>
        expression.Target.Is<MemberReferenceExpression>(memberReference => memberReference.IsTaskStartNew()) &&
        (expression.Arguments.First().Is<BinaryOperatorExpression>(binary => binary.GetActualType().IsFunc()) ||
        expression.Arguments.First().Is<CastExpression>(binary => binary.GetActualType().IsFunc()));
}
