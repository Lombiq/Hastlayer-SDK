namespace Hast.VhdlBuilder.Representation.Declaration;

public abstract class DataObjectBase : IDataObject
{
    public virtual DataObjectKind DataObjectKind { get; set; }
    public virtual string Name { get; set; }

    public abstract IDataObject ToReference();

    public abstract string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions);
}
