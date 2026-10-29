using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;

namespace Hast.Transformer.Vhdl.Models;

public class NullableRecord : Record
{
    public static readonly string IsNullFieldName = "IsNull".ToExtendedVhdlId();

    public NullableRecord()
    {
        var isNullField = new RecordField
        {
            DataType = KnownDataTypes.Boolean,
            Name = IsNullFieldName,
        };

        Fields.Add(isNullField);
    }

    public static RecordFieldAccess CreateIsNullFieldAccess(IDataObject recordInstance) =>
        new()
        {
            Instance = recordInstance,
            FieldName = IsNullFieldName,
        };
}
