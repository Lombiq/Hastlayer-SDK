using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Case : IVhdlElement
{
    public IVhdlElement Expression { get; set; }
    public IList<CaseWhen> Whens { get; } = new List<CaseWhen>();

    /// <summary>
    /// Gets or sets a value indicating whether the case expression is a matching case (case?) new to VHDL 2008.
    /// </summary>
    public bool IsMatching { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var builder = new StringBuilder();

        builder
            .Append("case");
        if (IsMatching) builder.Append('?');
        builder
            .Append(' ')
            .Append(Expression.ToVhdl(vhdlGenerationOptions))
            .Append(" is ")
            .Append(vhdlGenerationOptions.NewLineIfShouldFormat());

        foreach (var when in Whens)
        {
            builder.Append(when.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions));
        }

        builder.Append("end case");
        if (IsMatching) builder.Append('?');

        return Terminated.Terminate(builder.ToString(), vhdlGenerationOptions);
    }
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class CaseWhen : IBlockElement
{
    public IVhdlElement Expression { get; set; }
    public IList<IVhdlElement> Body { get; } = new List<IVhdlElement>();

    public CaseWhen() { }

    public CaseWhen(IVhdlElement expression, List<IVhdlElement> body)
    {
        Expression = expression;
        Body = body;
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        "when " + Expression.ToVhdl(vhdlGenerationOptions) + " => " + vhdlGenerationOptions.NewLineIfShouldFormat() +
        (Body.Count != 0 ?
            Body.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) :
            Terminated.Terminate(vhdlGenerationOptions.IndentIfShouldFormat() + "null", vhdlGenerationOptions));

    public static CaseWhen CreateOthers() => new() { Expression = "others".ToVhdlValue(KnownDataTypes.Identifier) };
}
