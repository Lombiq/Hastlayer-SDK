using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// An interface for sub-transformers that support only support specific node types.
/// </summary>
public interface ISpecificNodeTypeTransformer
{
    /// <summary>
    /// Returns whether the node type is usable for this service.
    /// </summary>
    bool IsSupported(AstNode node);
}
