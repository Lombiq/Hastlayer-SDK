using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// A service for transforming <see cref="Statement"/> nodes.
/// </summary>
public interface IStatementTransformer : IDependency
{
    /// <summary>
    /// Transforms <paramref name="statement"/> into the appropriate VHDL elements.
    /// </summary>
    void Transform(Statement statement, SubTransformerContext context);
}
