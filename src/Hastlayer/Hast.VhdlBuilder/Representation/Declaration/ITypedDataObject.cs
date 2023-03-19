namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Represents a VHDL data object with a type, such as a <see cref="Signal"/>.
/// </summary>
public interface ITypedDataObject : IDataObject
{
    /// <summary>
    /// Gets or sets the VHDL data type of the object.
    /// </summary>
    DataType DataType { get; set; }
}
