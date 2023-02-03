using Hast.VhdlBuilder.Representation.Declaration;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

/// <summary>
/// A record's field's access expression, i.e. myRecord.Member.
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class RecordFieldAccess : DataObjectBase
{
    private IDataObject _instance;

    public IDataObject Instance
    {
        get => _instance;
        set
        {
            _instance = value;
            DataObjectKind = value.DataObjectKind;
            Name = value.Name;
        }
    }

    public string FieldName { get; set; }

    public override IDataObject ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Instance.ToReference().ToVhdl(vhdlGenerationOptions) + "." + FieldName;
}
