using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Expression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Represents an enum data type.")]
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Represents an enum data type.")]
public class Enum : DataType
{
    public ICollection<Value> Values { get; } = new List<Value>();

    public Enum() => TypeCategory = DataTypeCategory.Composite;

    public Enum(IEnumerable<Value> values)
        : this()
    {
        if (values != null) Values.AddRange(values);
    }

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "type " + vhdlGenerationOptions.ShortenName(Name) + " is (" + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Values.ToVhdl(vhdlGenerationOptions, ", " + Environment.NewLine, string.Empty).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            ")",
            vhdlGenerationOptions);
}
