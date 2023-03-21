using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Transforms an expression into a VHDL element.
/// </summary>
public interface IExpressionTransformer : IDependency
{
    /// <summary>
    /// Transforms an expression into a VHDL element that can be used in place of the original expression. Be aware that
    /// <c>currentBlock</c>, being a reference, can change.
    /// </summary>
    /// <returns>
    /// A reference that can be used in place of the original expression. WARNING: don't re-use this handle in multiple
    /// state machine states! Even if it's the same expression it should be transformed for each state separately.
    /// </returns>
    IVhdlElement Transform(Expression expression, SubTransformerContext context);
}
