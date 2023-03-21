using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Terminated : IVhdlElement
{
    public IVhdlElement Element { get; set; }

    public Terminated()
    {
    }

    public Terminated(IVhdlElement element) => Element = element;

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminate(Element.ToVhdl(vhdlGenerationOptions), vhdlGenerationOptions);

    public static string Terminator(IVhdlGenerationOptions vhdlGenerationOptions) =>
        ";" + vhdlGenerationOptions.NewLineIfShouldFormat();

    public static string Terminate(string vhdl, IVhdlGenerationOptions vhdlGenerationOptions)
    {
        if (string.IsNullOrEmpty(vhdl)) return string.Empty;

        return vhdl.TrimEnd(Environment.NewLine.ToCharArray()).EndsWithOrdinal(";")
            ? vhdl
            : vhdl + Terminator(vhdlGenerationOptions);
    }
}

public static class TerminatedExtensions
{
    public static IVhdlElement Terminate(this IVhdlElement element) => new Terminated(element);
}
