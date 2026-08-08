using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(Hast.VhdlBuilder.Representation.VhdlGenerationOptions.Debug)}")]
public class Function : ISubProgram
{
    public string Name { get; set; }
    public IList<FunctionArgument> Arguments { get; } = [];
    public DataType ReturnType { get; set; }
    public IList<IVhdlElement> Declarations { get; } = [];
    public IList<IVhdlElement> Body { get; } = [];

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var name = vhdlGenerationOptions.ShortenName(Name);
        return Terminated.Terminate(
            "function " + name +
            " (" + Arguments.ToVhdl(vhdlGenerationOptions, "; ", string.Empty) + ") " + vhdlGenerationOptions.NewLineIfShouldFormat() +
            "return " + ReturnType.Name + " is " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Declarations.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
                (Declarations != null && Declarations.Any() ? " " : string.Empty) +
            "begin " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Body.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end " + name,
            vhdlGenerationOptions);
    }
}

public class FunctionArgument : TypedDataObjectBase
{
    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        (DataObjectKind.ToString() ?? string.Empty) +
        vhdlGenerationOptions.ShortenName(Name) +
        ": " +
        DataType.ToVhdl(vhdlGenerationOptions);
}
