using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class While : IBlockElement
{
    public IVhdlElement Condition { get; set; }
    public IList<IVhdlElement> Body { get; } = new List<IVhdlElement>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "while " + Condition.ToVhdl(vhdlGenerationOptions) + " loop " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Body.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end loop",
            vhdlGenerationOptions);
}
