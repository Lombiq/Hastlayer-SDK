using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Expression;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class StdLogicVector : SizedDataType
{
    private Value _defaultValue;

    public override Value DefaultValue
    {
        get => _defaultValue ?? "others => '0'".ToVhdlValue(this);
        set => _defaultValue = value;
    }

    public StdLogicVector(DataType baseType)
        : base(baseType)
    {
    }

    public StdLogicVector(SizedDataType previous)
        : base(previous)
    {
        SizeNumber = previous.SizeNumber;
        SizeExpression = previous.SizeExpression;
    }

    public StdLogicVector()
    {
        Name = "std_logic_vector";
        TypeCategory = DataTypeCategory.Array;
    }
}
