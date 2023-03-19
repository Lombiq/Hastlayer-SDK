using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Variable : TypedDataObject
{
    public bool Shared { get; set; }

    public Variable() => DataObjectKind = DataObjectKind.Variable;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        (Shared ? "shared " : string.Empty) + base.ToVhdl(vhdlGenerationOptions);
}
