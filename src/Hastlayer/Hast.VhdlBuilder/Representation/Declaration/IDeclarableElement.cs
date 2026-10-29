using System.Collections.Generic;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Represents an element that contains type declarations.
/// </summary>
public interface IDeclarableElement : IVhdlElement
{
    /// <summary>
    /// Gets the list of type declarations.
    /// </summary>
    IList<IVhdlElement> Declarations { get; }
}
