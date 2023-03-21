namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Represents a VHDL element with a name, such as <see cref="Entity"/>.
/// </summary>
public interface INamedElement : IVhdlElement
{
    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    string Name { get; set; }
}
