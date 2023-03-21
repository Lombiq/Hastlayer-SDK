using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class IfElse<T> : If<T>, IVhdlElement
    where T : IVhdlElement
{
    public IList<If<T>> ElseIfs { get; } = new List<If<T>>();
    public T Else { get; set; }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var vhdl =
            "if (" + Condition.ToVhdl(vhdlGenerationOptions) + ") then " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                True.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions);

        foreach (var elseIf in ElseIfs)
        {
            // More than 1 "else if" statement is very rare, so it isn't worth the cost of a StringBuilder.
#pragma warning disable S1643 // Strings should not be concatenated using '+' in a loop
            vhdl +=
                "elsif (" + elseIf.Condition.ToVhdl(vhdlGenerationOptions) + ") then " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                    elseIf.True.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions);
#pragma warning restore S1643 // Strings should not be concatenated using '+' in a loop
        }

        if (!Equals(Else, default(T)))
        {
            vhdl += "else " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Else.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions);
        }

        vhdl += "end if";

        return Terminated.Terminate(vhdl, vhdlGenerationOptions);
    }
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class IfElse : IfElse<IVhdlElement>
{
}
