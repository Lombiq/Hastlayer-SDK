using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Return : IVhdlElement
{
    public IVhdlElement Expression { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "return" +
            (Expression != null ? Expression.ToVhdl(vhdlGenerationOptions) : string.Empty),
            vhdlGenerationOptions);
}
