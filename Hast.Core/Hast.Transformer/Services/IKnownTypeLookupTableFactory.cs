using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.TypeSystem;

namespace Hast.Transformer.Services;

/// <summary>
/// A service for creating new <see cref="IKnownTypeLookupTable"/> instances.
/// </summary>
public interface IKnownTypeLookupTableFactory : IDependency
{
    /// <summary>
    /// Returns a new <see cref="IKnownTypeLookupTable"/> instance that has the <paramref name="compilation"/>.
    /// </summary>
    IKnownTypeLookupTable Create(ICompilation compilation);
}
