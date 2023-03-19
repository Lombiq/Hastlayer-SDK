using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Procedure : ISubProgram
{
    public string Name { get; set; }
    public IList<ProcedureParameter> Parameters { get; } = new List<ProcedureParameter>();
    public IList<IVhdlElement> Declarations { get; } = new List<IVhdlElement>();
    public IList<IVhdlElement> Body { get; } = new List<IVhdlElement>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var name = vhdlGenerationOptions.ShortenName(Name);
        return Terminated.Terminate(
            "procedure " + name +
            (Parameters.Count > 0 ? " (" : " ") + vhdlGenerationOptions.NewLineIfShouldFormat() +
            // Out params at the end.
            Parameters.OrderBy(parameter => parameter.ParameterType).ToVhdl(vhdlGenerationOptions, "; ", string.Empty) +
            (Parameters.Count > 0 ? ")" : string.Empty) +
            " is " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Declarations.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
                (Declarations != null && Declarations.Any() ? " " : string.Empty) +
            "begin " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Body.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end procedure " + name,
            vhdlGenerationOptions);
    }
}

public enum ProcedureParameterType
{
    In,
    InOut,
    Out,
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class ProcedureParameter : TypedDataObjectBase
{
    public ProcedureParameterType ParameterType { get; set; }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        (DataObjectKind.ToString() ?? string.Empty) +
        vhdlGenerationOptions.ShortenName(Name) +
        ": " +
        ParameterType +
        " " +
        DataType.ToVhdl(vhdlGenerationOptions);
}
