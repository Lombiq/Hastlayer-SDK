using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Generic : IVhdlElement
{
    public IList<GenericItem> Items { get; } = new List<GenericItem>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var values = Items != null
            ? Items
                .ToVhdl(vhdlGenerationOptions, Terminated.Terminator(vhdlGenerationOptions))
                .IndentLinesIfShouldFormat(vhdlGenerationOptions)
            : string.Empty;
        return Terminated.Terminate(
            $"generic ({vhdlGenerationOptions.NewLineIfShouldFormat()}{values})", vhdlGenerationOptions);
    }
}

public class GenericItem : DataObjectBase
{
    public Value Value { get; set; }

    public GenericItem() => DataObjectKind = DataObjectKind.Constant;

    public override IDataObject ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        vhdlGenerationOptions.ShortenName(Name) +
        ": " +
        Value.DataType.ToVhdl(vhdlGenerationOptions) +
        " := " +
        Value.ToVhdl(vhdlGenerationOptions);
}
