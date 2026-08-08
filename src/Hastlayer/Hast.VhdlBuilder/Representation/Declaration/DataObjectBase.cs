using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public abstract class DataObjectBase : IDataObject
{
    public virtual DataObjectKind DataObjectKind { get; set; }
    public virtual string Name { get; set; }

    public abstract IDataObject ToReference();

    public abstract string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions);
}
