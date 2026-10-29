using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class Process : ISubProgram
{
    public string Label { get; set; }

    public string Name { get => Label; set => Label = value; }

    public IList<IDataObject> SensitivityList { get; } = [];
    public IList<IVhdlElement> Declarations { get; } = [];
    public IList<IVhdlElement> Body { get; } = [];

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            (!string.IsNullOrEmpty(Label) ? vhdlGenerationOptions.ShortenName(Label) + ": " : string.Empty) +
            "process (" +
                string.Join("; ", SensitivityList.Select(signal => vhdlGenerationOptions.ShortenName(signal.Name))) +
            ") " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Declarations.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "begin " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Body.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end process",
            vhdlGenerationOptions);
}
