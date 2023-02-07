using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Attribute : DataType
{
    public DataType ValueType { get; set; }

    public Attribute() => TypeCategory = DataTypeCategory.Identifier;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            $"attribute {vhdlGenerationOptions.ShortenName(Name)}: {ValueType.ToReference().ToVhdl(vhdlGenerationOptions)}",
            vhdlGenerationOptions);
}
