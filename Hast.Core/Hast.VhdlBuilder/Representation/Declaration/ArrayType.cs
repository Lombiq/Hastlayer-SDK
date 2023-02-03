using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class ArrayType : ArrayTypeBase // Not named "Array" to avoid naming clash with System.Array.
{
    public DataType RangeType { get; set; } = KnownDataTypes.UnrangedInt;
    public int MaxLength { get; set; }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var shortName = vhdlGenerationOptions.ShortenName(Name);
        var range = MaxLength > 0
            ? MaxLength.ToTechnicalString() + " downto 0"
            : RangeType.ToReference().ToVhdl(vhdlGenerationOptions) + " range <>";
        var vhdl = ElementType.ToReference().ToVhdl(vhdlGenerationOptions);

        return Terminated.Terminate(
            FormattableString.Invariant($"type {shortName} is array ({range}) of {vhdl}"),
            vhdlGenerationOptions);
    }
}
