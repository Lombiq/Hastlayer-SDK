using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class ComponentInstance : IVhdlElement
{
    public Component Component { get; set; }
    public string Label { get; set; }
    public IList<PortMapping> PortMappings { get; } = new List<PortMapping>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => Terminated.Terminate(
            Label + " : " + vhdlGenerationOptions.ShortenName(Component.Name) + vhdlGenerationOptions.NewLineIfShouldFormat() +

                "port map (" + vhdlGenerationOptions.NewLineIfShouldFormat() +
                    PortMappings
                        .ToVhdl(vhdlGenerationOptions, Terminated.Terminator(vhdlGenerationOptions))
                        .IndentLinesIfShouldFormat(vhdlGenerationOptions) +
                Terminated.Terminate(")", vhdlGenerationOptions) +

            ")",
            vhdlGenerationOptions);
}

public class PortMapping : IVhdlElement
{
    public string From { get; set; }
    public string To { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        vhdlGenerationOptions.ShortenName(From) + " => " + vhdlGenerationOptions.ShortenName(To);
}
