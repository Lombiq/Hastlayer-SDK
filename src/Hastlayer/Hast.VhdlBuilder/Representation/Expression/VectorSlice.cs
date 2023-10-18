using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class VectorSlice : IVhdlElement
{
    public IVhdlElement Vector { get; set; }

    public int IndexFrom { get; set; }
    public int IndexTo { get; set; }

    public bool IsDownTo { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var vhdl = Vector.ToVhdl(vhdlGenerationOptions);
        var direction = IsDownTo ? "downto" : "to";
        return StringHelper.CreateInvariant($"{vhdl}({IndexFrom} {direction} {IndexTo})");
    }
}
