using Hast.VhdlBuilder.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Declaration;

[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Yes, they do.")]
public static class KnownDataTypes
{
    // There is a private field and a public one for each type because the DefaultValue construction needs the type
    // itself too.

    private static readonly DataType _bit = new() { TypeCategory = DataTypeCategory.Character, Name = "bit" };

    public static readonly DataType Bit = new(_bit)
    {
        DefaultValue = "0".ToVhdlValue(_bit),
    };

    private static readonly DataType _boolean = new() { TypeCategory = DataTypeCategory.Identifier, Name = "boolean" };

    public static readonly DataType Boolean = new(_boolean)
    {
        DefaultValue = "false".ToVhdlValue(_boolean),
    };

    private static readonly SizedDataType _binaryString = new()
    {
        TypeCategory = DataTypeCategory.Scalar,
        Name = "signed",
        SizeNumber = 16,
    };

    public static SizedDataType BinaryString { get; } = new SizedDataType(_binaryString)
    {
        DefaultValue = default(short).ToTechnicalString().ToVhdlValue(_int16),
    };

    private static readonly DataType _character = new() { TypeCategory = DataTypeCategory.Character, Name = "character" };

    public static readonly DataType Character = new(_character)
    {
        DefaultValue = default(char).ToString(CultureInfo.InvariantCulture).ToVhdlValue(_character),
    };

    public static readonly Identifier Identifier = new();

    private static readonly DataType _unrangedInt = new() { TypeCategory = DataTypeCategory.Scalar, Name = "integer" };

    public static readonly DataType UnrangedInt = new(_unrangedInt)
    {
        DefaultValue = default(int).ToVhdlValue(_unrangedInt),
    };

    private static readonly SizedDataType _int8 = new()
    {
        TypeCategory = DataTypeCategory.Scalar,
        Name = "signed",
        SizeNumber = 8,
    };

    public static readonly SizedDataType Int8 = new(_int8)
    {
        DefaultValue = default(sbyte).ToTechnicalString().ToVhdlValue(_int8),
    };

    private static readonly SizedDataType _int16 = new(_int8) { SizeNumber = 16 };

    public static readonly SizedDataType Int16 = new(_int16)
    {
        DefaultValue = default(short).ToTechnicalString().ToVhdlValue(_int16),
    };

    private static readonly SizedDataType _int32 = new(_int16) { SizeNumber = 32 };

    public static readonly SizedDataType Int32 = new(_int32)
    {
        DefaultValue = default(int).ToVhdlValue(_int32),
    };

    private static readonly SizedDataType _int64 = new(_int16) { SizeNumber = 64 };

    public static readonly SizedDataType Int64 = new(_int64)
    {
        DefaultValue = default(long).ToTechnicalString().ToVhdlValue(_int64),
    };

    public static readonly SizedDataType[] SignedIntegers = new[] { Int8, Int16, Int32, Int64 };

    private static readonly SizedDataType _uint8 = new(_int16) { Name = "unsigned", SizeNumber = 8 };

    public static readonly SizedDataType UInt8 = new(_uint8)
    {
        DefaultValue = default(byte).ToTechnicalString().ToVhdlValue(_uint8),
    };

    private static readonly SizedDataType _uint16 = new(_uint8) { SizeNumber = 16 };

    public static readonly SizedDataType UInt16 = new(_uint16)
    {
        DefaultValue = default(ushort).ToTechnicalString().ToVhdlValue(_uint16),
    };

    private static readonly SizedDataType _uint32 = new(_uint16) { SizeNumber = 32 };

    public static readonly SizedDataType UInt32 = new(_uint32)
    {
        DefaultValue = default(uint).ToTechnicalString().ToVhdlValue(_uint32),
    };

    private static readonly SizedDataType _uint64 = new(_uint16) { SizeNumber = 64 };

    public static readonly SizedDataType UInt64 = new(_uint64)
    {
        DefaultValue = default(ulong).ToTechnicalString().ToVhdlValue(_uint64),
    };

    public static readonly SizedDataType[] UnsignedIntegers = new[] { UInt8, UInt16, UInt32, UInt64 };

    public static readonly IReadOnlyList<SizedDataType> Integers = SignedIntegers.Union(UnsignedIntegers).ToArray();

    private static readonly DataType _stdLogic = new() { TypeCategory = DataTypeCategory.Character, Name = "std_logic" };

    public static readonly DataType StdLogic = new(_stdLogic)
    {
        DefaultValue = "0".ToVhdlValue(_stdLogic),
    };

    private static readonly StdLogicVector _stdLogicVector32 = new() { SizeNumber = 32 };
    public static readonly StdLogicVector StdLogicVector32 = new(_stdLogicVector32);

    private static readonly DataType _unrangedString = new String();

    public static readonly DataType UnrangedString = new(_unrangedString)
    {
        DefaultValue = default(string).ToVhdlValue(_unrangedString),
    };

    private static readonly DataType _real = new() { TypeCategory = DataTypeCategory.Scalar, Name = "real" };

    public static readonly DataType Real = new(_real)
    {
        DefaultValue = default(double).ToString(CultureInfo.InvariantCulture).ToVhdlValue(_real),
    };

    public static readonly DataType Void = new() { TypeCategory = DataTypeCategory.Identifier, Name = "void" };

    public static Attribute DontTouchAttribute { get; set; } = new Attribute
    {
        Name = "dont_touch",
        ValueType = UnrangedString,
    };
}
