using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class SizedDataType : DataType
{
    public int SizeNumber { get; set; }
    public IVhdlElement SizeExpression { get; set; }

    public SizedDataType(DataType baseType)
        : base(baseType)
    {
    }

    public SizedDataType(SizedDataType previous)
        : base(previous)
    {
        SizeNumber = previous.SizeNumber;
        SizeExpression = previous.SizeExpression;
    }

    public SizedDataType()
    {
    }

    public override DataType ToReference() => this;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        const int hasNumericSize = 1;
        const int hasSizeExpression = 2;
        const int hasBoth = hasNumericSize + hasSizeExpression;

        var state = (SizeNumber > 0 ? hasNumericSize : 0) + (SizeExpression != null ? hasSizeExpression : 0);
        return state switch
        {
            hasNumericSize => FormattableString.Invariant($"{Name}({SizeNumber - 1} downto 0)"),
            hasSizeExpression => FormattableString.Invariant($"{Name}({SizeExpression.ToVhdl(vhdlGenerationOptions)} downto 0)"),
            hasBoth => throw new InvalidOperationException(
                "VHDL sized data types should have their size specified either as an integer value or as an expression, but not both."),
            _ => Name,
        };
    }

    public override int GetSize() => SizeNumber;

    public override bool Equals(object obj)
    {
        var otherType = obj as SizedDataType;
        if (otherType == null) return false;
        return base.Equals(obj) &&
            (SizeExpression == null ? SizeNumber == otherType.SizeNumber : SizeExpression.ToVhdl() == otherType.SizeExpression.ToVhdl());
    }

    public override int GetHashCode() =>
        FormattableString.Invariant($"{Name}{TypeCategory}{SizeNumber}")
            .GetHashCode(StringComparison.InvariantCulture);
}
