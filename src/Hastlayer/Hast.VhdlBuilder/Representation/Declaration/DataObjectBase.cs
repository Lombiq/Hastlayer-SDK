using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{DebugDisplay,nq}")]
public abstract class DataObjectBase : IDataObject
{
    public virtual DataObjectKind DataObjectKind { get; set; }
    public virtual string Name { get; set; }

    protected string DebugDisplay => ToVhdl(VhdlGenerationOptions.Debug);

    public abstract IDataObject ToReference();

    public abstract string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions);
}
