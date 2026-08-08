using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class Component : INamedElement
{
    public string Name { get; set; }
    public IList<Port> Ports { get; } = [];

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var name = vhdlGenerationOptions.ShortenName(Name);
        return Terminated.Terminate(
            "component " + name + vhdlGenerationOptions.NewLineIfShouldFormat() +

                "port(" + vhdlGenerationOptions.NewLineIfShouldFormat() +
                    Ports
                        .ToVhdl(vhdlGenerationOptions, Terminated.Terminator(vhdlGenerationOptions))
                        .IndentLinesIfShouldFormat(vhdlGenerationOptions) +
                Terminated.Terminate(")", vhdlGenerationOptions) +

            "end " + name,
            vhdlGenerationOptions);
    }
}
