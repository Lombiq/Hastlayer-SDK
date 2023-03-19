using Hast.VhdlBuilder.Representation.Declaration;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Assignment : IVhdlElement
{
    public IDataObject AssignTo { get; set; }
    public IVhdlElement Expression { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            AssignTo.ToReference().ToVhdl(vhdlGenerationOptions) +
            (AssignTo.DataObjectKind == DataObjectKind.Variable ? " := " : " <= ") +
            Expression.ToVhdl(vhdlGenerationOptions),
            vhdlGenerationOptions);
}
