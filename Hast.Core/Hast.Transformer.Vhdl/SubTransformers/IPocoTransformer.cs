using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers;

/// <summary>
/// Transformer for processing POCOs (Plain Old C# Object) to handle e.g. properties.
/// </summary>
public interface IPocoTransformer : IDependency, ISpecificNodeTypeTransformer
{
    /// <summary>
    /// Transforms the <paramref name="typeDeclaration"/> of the class into matching member records.
    /// </summary>
    Task<IMemberTransformerResult> TransformAsync(TypeDeclaration typeDeclaration, IVhdlTransformationContext context);
}
