using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Binary : IVhdlElement
{
    public IVhdlElement Left { get; set; }
    public BinaryOperator Operator { get; set; }
    public IVhdlElement Right { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Left.ToVhdl(vhdlGenerationOptions) +
        " " + Operator.ToVhdl(vhdlGenerationOptions) + " " +
        Right.ToVhdl(vhdlGenerationOptions);
}
