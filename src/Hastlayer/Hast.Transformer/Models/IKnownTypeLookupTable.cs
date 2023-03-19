using ICSharpCode.Decompiler.TypeSystem;

namespace Hast.Transformer.Models;

/// <summary>
/// Retrieves the <see cref="IType"/> of a known type.
/// </summary>
public interface IKnownTypeLookupTable
{
    /// <summary>
    /// Retrieves the <see cref="IType"/> of a known type.
    /// </summary>
    IType Lookup(KnownTypeCode typeCode);
}
