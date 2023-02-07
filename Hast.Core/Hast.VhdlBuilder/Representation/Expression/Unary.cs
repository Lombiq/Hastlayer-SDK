using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Unary : IVhdlElement
{
    public IVhdlElement Expression { get; set; }
    public UnaryOperator Operator { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Operator.ToVhdl(vhdlGenerationOptions) + Expression.ToVhdl(vhdlGenerationOptions);
}
