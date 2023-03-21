using Hast.Common.Interfaces;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Expands binary operator expressions using the remainder operator (%) to a division-subtraction, i.e. <c>a % b = a –
/// a / b * b</c>. This is needed because for some reason the <c>rem</c> operator of VHDL produces the same results as
/// the <c>mod</c> operator (for differences see <see
/// href="https://stackoverflow.com/questions/25848879/difference-between-mod-and-rem-operators-in-vhdl"/>.).
/// </summary>
public interface IRemainderOperatorExpressionsExpander : IDependency
{
    /// <summary>
    /// Expands binary operator expressions using the remainder operator (%) to a division-subtraction.
    /// </summary>
    void ExpandRemainderOperatorExpressions(SyntaxTree syntaxTree);
}
