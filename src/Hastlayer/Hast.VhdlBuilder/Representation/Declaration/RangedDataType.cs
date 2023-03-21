using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class RangedDataType : DataType
{
    public int RangeMin { get; set; }
    public int RangeMax { get; set; }

    public RangedDataType(DataType baseType)
        : base(baseType)
    {
    }

    public RangedDataType(RangedDataType previous)
        : base(previous)
    {
        RangeMin = previous.RangeMin;
        RangeMax = previous.RangeMax;
    }

    public RangedDataType()
    {
    }

    public override DataType ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        FormattableString.Invariant($"{Name} range {RangeMin} to {RangeMax}");

    public override bool Equals(object obj)
    {
        var otherType = obj as RangedDataType;
        if (otherType == null) return false;
        return base.Equals(obj) && RangeMin == otherType.RangeMin && RangeMin == otherType.RangeMax;
    }

    public override int GetHashCode() =>
        FormattableString.Invariant($"{Name}{TypeCategory}{RangeMin}{RangeMax}")
            .GetHashCode(StringComparison.InvariantCulture);
}
