using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Helpers;

internal static class RecordHelper
{
    public static NullableRecord CreateNullableRecord(string name, IEnumerable<RecordField> fields)
    {
        var record = new NullableRecord { Name = name };
        record.Fields.AddRange(fields);
        return record;
    }
}
