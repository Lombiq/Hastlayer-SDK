using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Intentional.")]
public class String : DataType
{
    public int Length { get; set; }

    public String()
    {
        Name = "string";
        TypeCategory = DataTypeCategory.Array;
    }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Length > 0 ? StringHelper.CreateInvariant($"string(1 to {Length})") : "string";
}
