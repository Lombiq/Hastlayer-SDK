using System.Collections.Generic;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Represents a VHDL element that contains a body.
/// </summary>
public interface IBlockElement : IVhdlElement
{
    /// <summary>
    /// Gets the body of the element.
    /// </summary>
    IList<IVhdlElement> Body { get; }
}

public static class BlockElementExtensions
{
    public static void Add(this IBlockElement blockElement, IVhdlElement element) => blockElement.Body.Add(element);
}
