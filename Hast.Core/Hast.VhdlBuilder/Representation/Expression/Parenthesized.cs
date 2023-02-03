using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Parenthesized : IVhdlElement
{
    public IVhdlElement Target { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => "(" + Target.ToVhdl(vhdlGenerationOptions) + ")";
}
