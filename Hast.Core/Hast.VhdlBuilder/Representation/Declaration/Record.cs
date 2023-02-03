using Hast.VhdlBuilder.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Record : DataType
{
    public IList<RecordField> Fields { get; } = new List<RecordField>();

    public Record() => TypeCategory = DataTypeCategory.Composite;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "type " + vhdlGenerationOptions.ShortenName(Name) + " is record " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                Fields.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end record",
            vhdlGenerationOptions);
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class RecordField : TypedDataObjectBase
{
    public RecordField() => DataObjectKind = DataObjectKind.Variable;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            vhdlGenerationOptions.ShortenName(Name) +
            (DataType != null ? ": " + DataType.ToReference().ToVhdl(vhdlGenerationOptions) : string.Empty),
            vhdlGenerationOptions);
}
