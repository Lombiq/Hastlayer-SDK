namespace Hast.VhdlBuilder.Representation.Declaration;

public enum DataObjectKind
{
    Constant,
    Variable,
    Signal,
    File,
}

/// <summary>
/// Represents a <see href="https://surf-vhdl.com/vhdl-syntax-web-course-surf-vhdl/vhdl-types-of-data-object/"> VHDL
/// data object</see>.
/// </summary>
public interface IDataObject : INamedElement, IReferenceableDeclaration<IDataObject>
{
    /// <summary>
    /// Gets or sets the kind of the data object.
    /// </summary>
    DataObjectKind DataObjectKind { get; set; }
}
