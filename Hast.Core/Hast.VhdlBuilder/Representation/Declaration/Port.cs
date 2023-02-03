using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

public enum PortMode
{
    In,
    Out,
    Buffer,
    InOut,
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Port : TypedDataObjectBase
{
    public PortMode Mode { get; set; }

    public Port() => DataObjectKind = DataObjectKind.Signal;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        vhdlGenerationOptions.ShortenName(Name) +
        ": " +
        Mode +
        " " +
        DataType.ToVhdl(vhdlGenerationOptions);
}
