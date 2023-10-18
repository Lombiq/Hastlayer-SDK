using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Represents an attribute data type.")]
public class Attribute : DataType
{
    public DataType ValueType { get; set; }

    public Attribute() => TypeCategory = DataTypeCategory.Identifier;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            $"attribute {vhdlGenerationOptions.ShortenName(Name)}: {ValueType.ToReference().ToVhdl(vhdlGenerationOptions)}",
            vhdlGenerationOptions);
}
