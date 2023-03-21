using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services;

/// <summary>
/// A service for creating new instances of <see cref="ITypeDeclarationLookupTable"/>.
/// </summary>
public interface ITypeDeclarationLookupTableFactory : IDependency
{
    /// <summary>
    /// Creates a new instance of <see cref="ITypeDeclarationLookupTable"/> containing all type declarations in the
    /// <paramref name="syntaxTree"/>.
    /// </summary>
    ITypeDeclarationLookupTable Create(SyntaxTree syntaxTree);
}
