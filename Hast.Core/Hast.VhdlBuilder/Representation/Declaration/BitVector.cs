using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class BitVector : SizedDataType
{
    public BitVector(DataType baseType)
        : base(baseType)
    {
    }

    public BitVector(SizedDataType previous)
        : base(previous)
    {
        SizeNumber = previous.SizeNumber;
        SizeExpression = previous.SizeExpression;
    }

    public BitVector()
    {
        Name = "bit_vector";
        TypeCategory = DataTypeCategory.Array;
    }
}
