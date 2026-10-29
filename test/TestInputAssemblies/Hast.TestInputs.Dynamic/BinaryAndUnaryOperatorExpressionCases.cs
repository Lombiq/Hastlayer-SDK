using Hast.Transformer.SimpleMemory;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable RedundantCast

namespace Hast.TestInputs.Dynamic;

/// <summary>
/// Test class to use all binary and unary operator expression variations. Needed to check whether proper type
/// conversions are implemented.
/// </summary>
/// <remarks>
/// <para>
/// Note that all the cases for u/long won't fit on the Nexys A7 so we need to split them. Ideally all the cases would
/// fit into a single design though. Hardware resource usage for each of these tests is as following:
/// - ByteBinaryOperatorExpressionVariations: 52%
/// - SbyteBinaryOperatorExpressionVariations: 64%
/// - ShortBinaryOperatorExpressionVariations: 64%
/// - UshortBinaryOperatorExpressionVariations: 55%
/// - IntBinaryOperatorExpressionVariations: 63%
/// - UintBinaryOperatorExpressionVariations: 82%
/// - LongBinaryOperatorExpressionVariationsLow: 71%
/// - LongBinaryOperatorExpressionVariationsHigh: 53%
/// - UlongBinaryOperatorExpressionVariationsLow: 33%
/// - UlongBinaryOperatorExpressionVariationsHigh: 33%
/// - AllUnaryOperatorExpressionVariations: 33%
///
/// While using the 8-number SaveResult() method actually slightly increases resource usage synthesis time is greatly
/// reduced (as opposed to calling the single-number SaveResult() for every number). <see
/// cref="BinaryAndUnaryOperatorExpressionCasesGenerator"/> can be used to generate these cases.
/// </para>
/// </remarks>
public class BinaryAndUnaryOperatorExpressionCases : DynamicTestInputBase
{
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand

    public void ByteBinaryOperatorExpressionVariations(int input)
    {
        var memory = CreateMemory(160);
        memory.WriteInt32(0, input);
        ByteBinaryOperatorExpressionVariations(memory);
    }

    public virtual void ByteBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadInt32(0);
        var byteLeft = (byte)input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, byteLeft << byteRight);
        SaveResult(memory, 2, byteLeft >> byteRight);
        SaveResult(
            memory,
            4,
            byteLeft + byteRight, // 4
            byteLeft - byteRight, // 6
            byteLeft * byteRight, // 8
            byteLeft / byteRight, // 10
            byteLeft % byteRight, // 12
            byteLeft & byteRight, // 14
            byteLeft | byteRight, // 16
            byteLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, byteLeft << sbyteRight);
        SaveResult(memory, 22, byteLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            byteLeft + sbyteRight, // 24
            byteLeft - sbyteRight, // 26
            byteLeft * sbyteRight, // 28
            byteLeft / sbyteRight, // 30
            byteLeft % sbyteRight, // 32
            byteLeft & sbyteRight, // 34
            byteLeft | sbyteRight, // 36
            byteLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, byteLeft << shortRight);
        SaveResult(memory, 42, byteLeft >> shortRight);
        SaveResult(
            memory,
            44,
            byteLeft + shortRight, // 44
            byteLeft - shortRight, // 46
            byteLeft * shortRight, // 48
            byteLeft / shortRight, // 50
            byteLeft % shortRight, // 52
            byteLeft & shortRight, // 54
            byteLeft | shortRight, // 56
            byteLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, byteLeft << ushortRight);
        SaveResult(memory, 62, byteLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            byteLeft + ushortRight, // 64
            byteLeft - ushortRight, // 66
            byteLeft * ushortRight, // 68
            byteLeft / ushortRight, // 70
            byteLeft % ushortRight, // 72
            byteLeft & ushortRight, // 74
            byteLeft | ushortRight, // 76
            byteLeft ^ ushortRight); // 78

        var intRight = input;
        SaveResult(memory, 80, byteLeft << intRight);
        SaveResult(memory, 82, byteLeft >> intRight);
        SaveResult(
            memory,
            84,
            byteLeft + intRight, // 84
            byteLeft - intRight, // 86
            byteLeft * intRight, // 88
            byteLeft / intRight, // 90
            byteLeft % intRight, // 92
            byteLeft & intRight, // 94
            byteLeft | intRight, // 96
            byteLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, byteLeft << (int)uintRight);
        SaveResult(memory, 102, byteLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            byteLeft + uintRight, // 104
            byteLeft - uintRight, // 106
            byteLeft * uintRight, // 108
            byteLeft / uintRight, // 110
            byteLeft % uintRight, // 112
            byteLeft & uintRight, // 114
            byteLeft | uintRight, // 116
            byteLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, byteLeft << (int)longRight);
        SaveResult(memory, 122, byteLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            byteLeft + longRight, // 124
            byteLeft - longRight, // 126
            byteLeft * longRight, // 128
            byteLeft / longRight, // 130
            byteLeft % longRight, // 132
            byteLeft & longRight, // 134
            byteLeft | longRight, // 136
            byteLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, byteLeft << (int)ulongRight);
        SaveResult(memory, 142, byteLeft >> (int)ulongRight);
        SaveResult(
            memory,
            144,
            (long)(byteLeft + ulongRight), // 144
            (long)(byteLeft - ulongRight), // 146
            (long)(byteLeft * ulongRight), // 148
            (long)(byteLeft / ulongRight), // 150
            (long)(byteLeft % ulongRight), // 152
            (long)(byteLeft & ulongRight), // 154
            (long)(byteLeft | ulongRight), // 156
            (long)(byteLeft ^ ulongRight)); // 158
    }

    public void SbyteBinaryOperatorExpressionVariations(int input)
    {
        var memory = CreateMemory(144);
        memory.WriteInt32(0, input);
        SbyteBinaryOperatorExpressionVariations(memory);
    }

    public virtual void SbyteBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadInt32(0);
        var sbyteLeft = (sbyte)input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, sbyteLeft << byteRight);
        SaveResult(memory, 2, sbyteLeft >> byteRight);
        SaveResult(
            memory,
            4,
            sbyteLeft + byteRight, // 4
            sbyteLeft - byteRight, // 6
            sbyteLeft * byteRight, // 8
            sbyteLeft / byteRight, // 10
            sbyteLeft % byteRight, // 12
            sbyteLeft & byteRight, // 14
            sbyteLeft | byteRight, // 16
            sbyteLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, sbyteLeft << sbyteRight);
        SaveResult(memory, 22, sbyteLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            sbyteLeft + sbyteRight, // 24
            sbyteLeft - sbyteRight, // 26
            sbyteLeft * sbyteRight, // 28
            sbyteLeft / sbyteRight, // 30
            sbyteLeft % sbyteRight, // 32
            sbyteLeft & sbyteRight, // 34
            sbyteLeft | sbyteRight, // 36
            sbyteLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, sbyteLeft << shortRight);
        SaveResult(memory, 42, sbyteLeft >> shortRight);
        SaveResult(
            memory,
            44,
            sbyteLeft + shortRight, // 44
            sbyteLeft - shortRight, // 46
            sbyteLeft * shortRight, // 48
            sbyteLeft / shortRight, // 50
            sbyteLeft % shortRight, // 52
            sbyteLeft & shortRight, // 54
            sbyteLeft | shortRight, // 56
            sbyteLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, sbyteLeft << ushortRight);
        SaveResult(memory, 62, sbyteLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            sbyteLeft + ushortRight, // 64
            sbyteLeft - ushortRight, // 66
            sbyteLeft * ushortRight, // 68
            sbyteLeft / ushortRight, // 70
            sbyteLeft % ushortRight, // 72
            sbyteLeft & ushortRight, // 74
            sbyteLeft | ushortRight, // 76
            sbyteLeft ^ ushortRight); // 78

        var intRight = input;
        SaveResult(memory, 80, sbyteLeft << intRight);
        SaveResult(memory, 82, sbyteLeft >> intRight);
        SaveResult(
            memory,
            84,
            sbyteLeft + intRight, // 84
            sbyteLeft - intRight, // 86
            sbyteLeft * intRight, // 88
            sbyteLeft / intRight, // 90
            sbyteLeft % intRight, // 92
            sbyteLeft & intRight, // 94
            sbyteLeft | intRight, // 96
            sbyteLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, sbyteLeft << (int)uintRight);
        SaveResult(memory, 102, sbyteLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            sbyteLeft + uintRight, // 104
            sbyteLeft - uintRight, // 106
            sbyteLeft * uintRight, // 108
            sbyteLeft / uintRight, // 110
            sbyteLeft % uintRight, // 112
            sbyteLeft & uintRight, // 114
            sbyteLeft | uintRight, // 116
            sbyteLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, sbyteLeft << (int)longRight);
        SaveResult(memory, 122, sbyteLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            sbyteLeft + longRight, // 124
            sbyteLeft - longRight, // 126
            sbyteLeft * longRight, // 128
            sbyteLeft / longRight, // 130
            sbyteLeft % longRight, // 132
            sbyteLeft & longRight, // 134
            sbyteLeft | longRight, // 136
            sbyteLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, sbyteLeft << (int)ulongRight);
        SaveResult(memory, 142, sbyteLeft >> (int)ulongRight);
    }

    public void ShortBinaryOperatorExpressionVariations(int input)
    {
        var memory = CreateMemory(144);
        memory.WriteInt32(0, input);
        ShortBinaryOperatorExpressionVariations(memory);
    }

    public virtual void ShortBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadInt32(0);
        var shortLeft = (short)input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, shortLeft << byteRight);
        SaveResult(memory, 2, shortLeft >> byteRight);
        SaveResult(
            memory,
            4,
            shortLeft + byteRight, // 4
            shortLeft - byteRight, // 6
            shortLeft * byteRight, // 8
            shortLeft / byteRight, // 10
            shortLeft % byteRight, // 12
            shortLeft & byteRight, // 14
            shortLeft | byteRight, // 16
            shortLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, shortLeft << sbyteRight);
        SaveResult(memory, 22, shortLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            shortLeft + sbyteRight, // 24
            shortLeft - sbyteRight, // 26
            shortLeft * sbyteRight, // 28
            shortLeft / sbyteRight, // 30
            shortLeft % sbyteRight, // 32
            shortLeft & sbyteRight, // 34
            shortLeft | sbyteRight, // 36
            shortLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, shortLeft << shortRight);
        SaveResult(memory, 42, shortLeft >> shortRight);
        SaveResult(
            memory,
            44,
            shortLeft + shortRight, // 44
            shortLeft - shortRight, // 46
            shortLeft * shortRight, // 48
            shortLeft / shortRight, // 50
            shortLeft % shortRight, // 52
            shortLeft & shortRight, // 54
            shortLeft | shortRight, // 56
            shortLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, shortLeft << ushortRight);
        SaveResult(memory, 62, shortLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            shortLeft + ushortRight, // 64
            shortLeft - ushortRight, // 66
            shortLeft * ushortRight, // 68
            shortLeft / ushortRight, // 70
            shortLeft % ushortRight, // 72
            shortLeft & ushortRight, // 74
            shortLeft | ushortRight, // 76
            shortLeft ^ ushortRight); // 78

        var intRight = input;
        SaveResult(memory, 80, shortLeft << intRight);
        SaveResult(memory, 82, shortLeft >> intRight);
        SaveResult(
            memory,
            84,
            shortLeft + intRight, // 84
            shortLeft - intRight, // 86
            shortLeft * intRight, // 88
            shortLeft / intRight, // 90
            shortLeft % intRight, // 92
            shortLeft & intRight, // 94
            shortLeft | intRight, // 96
            shortLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, shortLeft << (int)uintRight);
        SaveResult(memory, 102, shortLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            shortLeft + uintRight, // 104
            shortLeft - uintRight, // 106
            shortLeft * uintRight, // 108
            shortLeft / uintRight, // 110
            shortLeft % uintRight, // 112
            shortLeft & uintRight, // 114
            shortLeft | uintRight, // 116
            shortLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, shortLeft << (int)longRight);
        SaveResult(memory, 122, shortLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            shortLeft + longRight, // 124
            shortLeft - longRight, // 126
            shortLeft * longRight, // 128
            shortLeft / longRight, // 130
            shortLeft % longRight, // 132
            shortLeft & longRight, // 134
            shortLeft | longRight, // 136
            shortLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, shortLeft << (int)ulongRight);
        SaveResult(memory, 142, shortLeft >> (int)ulongRight);
    }

    public void UshortBinaryOperatorExpressionVariations(int input)
    {
        var memory = CreateMemory(160);
        memory.WriteInt32(0, input);
        UshortBinaryOperatorExpressionVariations(memory);
    }

    public virtual void UshortBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadInt32(0);
        var ushortLeft = (ushort)input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, ushortLeft << byteRight);
        SaveResult(memory, 2, ushortLeft >> byteRight);
        SaveResult(
            memory,
            4,
            ushortLeft + byteRight, // 4
            ushortLeft - byteRight, // 6
            ushortLeft * byteRight, // 8
            ushortLeft / byteRight, // 10
            ushortLeft % byteRight, // 12
            ushortLeft & byteRight, // 14
            ushortLeft | byteRight, // 16
            ushortLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, ushortLeft << sbyteRight);
        SaveResult(memory, 22, ushortLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            ushortLeft + sbyteRight, // 24
            ushortLeft - sbyteRight, // 26
            ushortLeft * sbyteRight, // 28
            ushortLeft / sbyteRight, // 30
            ushortLeft % sbyteRight, // 32
            ushortLeft & sbyteRight, // 34
            ushortLeft | sbyteRight, // 36
            ushortLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, ushortLeft << shortRight);
        SaveResult(memory, 42, ushortLeft >> shortRight);
        SaveResult(
            memory,
            44,
            ushortLeft + shortRight, // 44
            ushortLeft - shortRight, // 46
            ushortLeft * shortRight, // 48
            ushortLeft / shortRight, // 50
            ushortLeft % shortRight, // 52
            ushortLeft & shortRight, // 54
            ushortLeft | shortRight, // 56
            ushortLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, ushortLeft << ushortRight);
        SaveResult(memory, 62, ushortLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            ushortLeft + ushortRight, // 64
            ushortLeft - ushortRight, // 66
            ushortLeft * ushortRight, // 68
            ushortLeft / ushortRight, // 70
            ushortLeft % ushortRight, // 72
            ushortLeft & ushortRight, // 74
            ushortLeft | ushortRight, // 76
            ushortLeft ^ ushortRight); // 78

        var intRight = input;
        SaveResult(memory, 80, ushortLeft << intRight);
        SaveResult(memory, 82, ushortLeft >> intRight);
        SaveResult(
            memory,
            84,
            ushortLeft + intRight, // 84
            ushortLeft - intRight, // 86
            ushortLeft * intRight, // 88
            ushortLeft / intRight, // 90
            ushortLeft % intRight, // 92
            ushortLeft & intRight, // 94
            ushortLeft | intRight, // 96
            ushortLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, ushortLeft << (int)uintRight);
        SaveResult(memory, 102, ushortLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            ushortLeft + uintRight, // 104
            ushortLeft - uintRight, // 106
            ushortLeft * uintRight, // 108
            ushortLeft / uintRight, // 110
            ushortLeft % uintRight, // 112
            ushortLeft & uintRight, // 114
            ushortLeft | uintRight, // 116
            ushortLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, ushortLeft << (int)longRight);
        SaveResult(memory, 122, ushortLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            ushortLeft + longRight, // 124
            ushortLeft - longRight, // 126
            ushortLeft * longRight, // 128
            ushortLeft / longRight, // 130
            ushortLeft % longRight, // 132
            ushortLeft & longRight, // 134
            ushortLeft | longRight, // 136
            ushortLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, ushortLeft << (int)ulongRight);
        SaveResult(memory, 142, ushortLeft >> (int)ulongRight);
        SaveResult(
            memory,
            144,
            (long)(ushortLeft + ulongRight), // 144
            (long)(ushortLeft - ulongRight), // 146
            (long)(ushortLeft * ulongRight), // 148
            (long)(ushortLeft / ulongRight), // 150
            (long)(ushortLeft % ulongRight), // 152
            (long)(ushortLeft & ulongRight), // 154
            (long)(ushortLeft | ulongRight), // 156
            (long)(ushortLeft ^ ulongRight)); // 158
    }

    public void IntBinaryOperatorExpressionVariations(int input)
    {
        var memory = CreateMemory(144);
        memory.WriteInt32(0, input);
        IntBinaryOperatorExpressionVariations(memory);
    }

    public virtual void IntBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadInt32(0);
        var intLeft = input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, intLeft << byteRight);
        SaveResult(memory, 2, intLeft >> byteRight);
        SaveResult(
            memory,
            4,
            intLeft + byteRight, // 4
            intLeft - byteRight, // 6
            intLeft * byteRight, // 8
            intLeft / byteRight, // 10
            intLeft % byteRight, // 12
            intLeft & byteRight, // 14
            intLeft | byteRight, // 16
            intLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, intLeft << sbyteRight);
        SaveResult(memory, 22, intLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            intLeft + sbyteRight, // 24
            intLeft - sbyteRight, // 26
            intLeft * sbyteRight, // 28
            intLeft / sbyteRight, // 30
            intLeft % sbyteRight, // 32
            intLeft & sbyteRight, // 34
            intLeft | sbyteRight, // 36
            intLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, intLeft << shortRight);
        SaveResult(memory, 42, intLeft >> shortRight);
        SaveResult(
            memory,
            44,
            intLeft + shortRight, // 44
            intLeft - shortRight, // 46
            intLeft * shortRight, // 48
            intLeft / shortRight, // 50
            intLeft % shortRight, // 52
            intLeft & shortRight, // 54
            intLeft | shortRight, // 56
            intLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, intLeft << ushortRight);
        SaveResult(memory, 62, intLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            intLeft + ushortRight, // 64
            intLeft - ushortRight, // 66
            intLeft * ushortRight, // 68
            intLeft / ushortRight, // 70
            intLeft % ushortRight, // 72
            intLeft & ushortRight, // 74
            intLeft | ushortRight, // 76
            intLeft ^ ushortRight); // 78

        var intRight = input;
        SaveResult(memory, 80, intLeft << intRight);
        SaveResult(memory, 82, intLeft >> intRight);
        SaveResult(
            memory,
            84,
            intLeft + intRight, // 84
            intLeft - intRight, // 86
            intLeft * intRight, // 88
            intLeft / intRight, // 90
            intLeft % intRight, // 92
            intLeft & intRight, // 94
            intLeft | intRight, // 96
            intLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, intLeft << (int)uintRight);
        SaveResult(memory, 102, intLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            intLeft + uintRight, // 104
            intLeft - uintRight, // 106
            intLeft * uintRight, // 108
            intLeft / uintRight, // 110
            intLeft % uintRight, // 112
            intLeft & uintRight, // 114
            intLeft | uintRight, // 116
            intLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, intLeft << (int)longRight);
        SaveResult(memory, 122, intLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            intLeft + longRight, // 124
            intLeft - longRight, // 126
            intLeft * longRight, // 128
            intLeft / longRight, // 130
            intLeft % longRight, // 132
            intLeft & longRight, // 134
            intLeft | longRight, // 136
            intLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, intLeft << (int)ulongRight);
        SaveResult(memory, 142, intLeft >> (int)ulongRight);
    }

    public void UintBinaryOperatorExpressionVariations(uint input)
    {
        var memory = CreateMemory(160);
        memory.WriteUInt32(0, input);
        UintBinaryOperatorExpressionVariations(memory);
    }

    public virtual void UintBinaryOperatorExpressionVariations(SimpleMemory memory)
    {
        var input = memory.ReadUInt32(0);
        var uintLeft = input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, uintLeft << byteRight);
        SaveResult(memory, 2, uintLeft >> byteRight);
        SaveResult(
            memory,
            4,
            uintLeft + byteRight, // 4
            uintLeft - byteRight, // 6
            uintLeft * byteRight, // 8
            uintLeft / byteRight, // 10
            uintLeft % byteRight, // 12
            uintLeft & byteRight, // 14
            uintLeft | byteRight, // 16
            uintLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, uintLeft << sbyteRight);
        SaveResult(memory, 22, uintLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            uintLeft + sbyteRight, // 24
            uintLeft - sbyteRight, // 26
            uintLeft * sbyteRight, // 28
            uintLeft / sbyteRight, // 30
            uintLeft % sbyteRight, // 32
            uintLeft & sbyteRight, // 34
            uintLeft | sbyteRight, // 36
            uintLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, uintLeft << shortRight);
        SaveResult(memory, 42, uintLeft >> shortRight);
        SaveResult(
            memory,
            44,
            uintLeft + shortRight, // 44
            uintLeft - shortRight, // 46
            uintLeft * shortRight, // 48
            uintLeft / shortRight, // 50
            uintLeft % shortRight, // 52
            uintLeft & shortRight, // 54
            uintLeft | shortRight, // 56
            uintLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, uintLeft << ushortRight);
        SaveResult(memory, 62, uintLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            uintLeft + ushortRight, // 64
            uintLeft - ushortRight, // 66
            uintLeft * ushortRight, // 68
            uintLeft / ushortRight, // 70
            uintLeft % ushortRight, // 72
            uintLeft & ushortRight, // 74
            uintLeft | ushortRight, // 76
            uintLeft ^ ushortRight); // 78

        var intRight = (int)input;
        SaveResult(memory, 80, uintLeft << intRight);
        SaveResult(memory, 82, uintLeft >> intRight);
        SaveResult(
            memory,
            84,
            uintLeft + intRight, // 84
            uintLeft - intRight, // 86
            uintLeft * intRight, // 88
            uintLeft / intRight, // 90
            uintLeft % intRight, // 92
            uintLeft & intRight, // 94
            uintLeft | intRight, // 96
            uintLeft ^ intRight); // 98

        var uintRight = input;
        SaveResult(memory, 100, uintLeft << (int)uintRight);
        SaveResult(memory, 102, uintLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            uintLeft + uintRight, // 104
            uintLeft - uintRight, // 106
            uintLeft * uintRight, // 108
            uintLeft / uintRight, // 110
            uintLeft % uintRight, // 112
            uintLeft & uintRight, // 114
            uintLeft | uintRight, // 116
            uintLeft ^ uintRight); // 118

        var longRight = (long)input;
        SaveResult(memory, 120, uintLeft << (int)longRight);
        SaveResult(memory, 122, uintLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            uintLeft + longRight, // 124
            uintLeft - longRight, // 126
            uintLeft * longRight, // 128
            uintLeft / longRight, // 130
            uintLeft % longRight, // 132
            uintLeft & longRight, // 134
            uintLeft | longRight, // 136
            uintLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, uintLeft << (int)ulongRight);
        SaveResult(memory, 142, uintLeft >> (int)ulongRight);
        SaveResult(
            memory,
            144,
            (long)(uintLeft + ulongRight), // 144
            (long)(uintLeft - ulongRight), // 146
            (long)(uintLeft * ulongRight), // 148
            (long)(uintLeft / ulongRight), // 150
            (long)(uintLeft % ulongRight), // 152
            (long)(uintLeft & ulongRight), // 154
            (long)(uintLeft | ulongRight), // 156
            (long)(uintLeft ^ ulongRight)); // 158
    }

    public void LongBinaryOperatorExpressionVariationsLow(long input)
    {
        var memory = CreateMemory(144);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        LongBinaryOperatorExpressionVariationsLow(memory);
    }

    public virtual void LongBinaryOperatorExpressionVariationsLow(SimpleMemory memory)
    {
        long input = ((long)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);
        var longLeft = input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, longLeft << byteRight);
        SaveResult(memory, 2, longLeft >> byteRight);
        SaveResult(
            memory,
            4,
            longLeft + byteRight, // 4
            longLeft - byteRight, // 6
            longLeft * byteRight, // 8
            longLeft / byteRight, // 10
            longLeft % byteRight, // 12
            longLeft & byteRight, // 14
            longLeft | byteRight, // 16
            longLeft ^ byteRight); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, longLeft << sbyteRight);
        SaveResult(memory, 22, longLeft >> sbyteRight);
        SaveResult(
            memory,
            24,
            longLeft + sbyteRight, // 24
            longLeft - sbyteRight, // 26
            longLeft * sbyteRight, // 28
            longLeft / sbyteRight, // 30
            longLeft % sbyteRight, // 32
            longLeft & sbyteRight, // 34
            longLeft | sbyteRight, // 36
            longLeft ^ sbyteRight); // 38

        var shortRight = (short)input;
        SaveResult(memory, 40, longLeft << shortRight);
        SaveResult(memory, 42, longLeft >> shortRight);
        SaveResult(
            memory,
            44,
            longLeft + shortRight, // 44
            longLeft - shortRight, // 46
            longLeft * shortRight, // 48
            longLeft / shortRight, // 50
            longLeft % shortRight, // 52
            longLeft & shortRight, // 54
            longLeft | shortRight, // 56
            longLeft ^ shortRight); // 58

        var ushortRight = (ushort)input;
        SaveResult(memory, 60, longLeft << ushortRight);
        SaveResult(memory, 62, longLeft >> ushortRight);
        SaveResult(
            memory,
            64,
            longLeft + ushortRight, // 64
            longLeft - ushortRight, // 66
            longLeft * ushortRight, // 68
            longLeft / ushortRight, // 70
            longLeft % ushortRight, // 72
            longLeft & ushortRight, // 74
            longLeft | ushortRight, // 76
            longLeft ^ ushortRight); // 78
    }

    public void LongBinaryOperatorExpressionVariationsHigh(long input)
    {
        var memory = CreateMemory(144);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        LongBinaryOperatorExpressionVariationsHigh(memory);
    }

    public virtual void LongBinaryOperatorExpressionVariationsHigh(SimpleMemory memory)
    {
        long input = ((long)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);
        var longLeft = input;

        var intRight = (int)input;
        SaveResult(memory, 80, longLeft << intRight);
        SaveResult(memory, 82, longLeft >> intRight);
        SaveResult(
            memory,
            84,
            longLeft + intRight, // 84
            longLeft - intRight, // 86
            longLeft * intRight, // 88
            longLeft / intRight, // 90
            longLeft % intRight, // 92
            longLeft & intRight, // 94
            longLeft | intRight, // 96
            longLeft ^ intRight); // 98

        var uintRight = (uint)input;
        SaveResult(memory, 100, longLeft << (int)uintRight);
        SaveResult(memory, 102, longLeft >> (int)uintRight);
        SaveResult(
            memory,
            104,
            longLeft + uintRight, // 104
            longLeft - uintRight, // 106
            longLeft * uintRight, // 108
            longLeft / uintRight, // 110
            longLeft % uintRight, // 112
            longLeft & uintRight, // 114
            longLeft | uintRight, // 116
            longLeft ^ uintRight); // 118

        var longRight = input;
        SaveResult(memory, 120, longLeft << (int)longRight);
        SaveResult(memory, 122, longLeft >> (int)longRight);
        SaveResult(
            memory,
            124,
            longLeft + longRight, // 124
            longLeft - longRight, // 126
            longLeft * longRight, // 128
            longLeft / longRight, // 130
            longLeft % longRight, // 132
            longLeft & longRight, // 134
            longLeft | longRight, // 136
            longLeft ^ longRight); // 138

        var ulongRight = (ulong)input;
        SaveResult(memory, 140, longLeft << (int)ulongRight);
        SaveResult(memory, 142, longLeft >> (int)ulongRight);
    }

    public void UlongBinaryOperatorExpressionVariationsLow(ulong input)
    {
        var memory = CreateMemory(96);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        UlongBinaryOperatorExpressionVariationsLow(memory);
    }

    public virtual void UlongBinaryOperatorExpressionVariationsLow(SimpleMemory memory)
    {
        ulong input = ((ulong)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);
        var ulongLeft = input;

        var byteRight = (byte)input;
        SaveResult(memory, 0, (long)(ulongLeft << byteRight));
        SaveResult(memory, 2, (long)(ulongLeft >> byteRight));
        SaveResult(
            memory,
            4,
            (long)(ulongLeft + byteRight), // 4
            (long)(ulongLeft - byteRight), // 6
            (long)(ulongLeft * byteRight), // 8
            (long)(ulongLeft / byteRight), // 10
            (long)(ulongLeft % byteRight), // 12
            (long)(ulongLeft & byteRight), // 14
            (long)(ulongLeft | byteRight), // 16
            (long)(ulongLeft ^ byteRight)); // 18

        var sbyteRight = (sbyte)input;
        SaveResult(memory, 20, (long)(ulongLeft << sbyteRight));
        SaveResult(memory, 22, (long)(ulongLeft >> sbyteRight));

        var shortRight = (short)input;
        SaveResult(memory, 24, (long)(ulongLeft << shortRight));
        SaveResult(memory, 26, (long)(ulongLeft >> shortRight));

        var ushortRight = (ushort)input;
        SaveResult(memory, 28, (long)(ulongLeft << ushortRight));
        SaveResult(memory, 30, (long)(ulongLeft >> ushortRight));
        SaveResult(
            memory,
            32,
            (long)(ulongLeft + ushortRight), // 32
            (long)(ulongLeft - ushortRight), // 34
            (long)(ulongLeft * ushortRight), // 36
            (long)(ulongLeft / ushortRight), // 38
            (long)(ulongLeft % ushortRight), // 40
            (long)(ulongLeft & ushortRight), // 42
            (long)(ulongLeft | ushortRight), // 44
            (long)(ulongLeft ^ ushortRight)); // 46
    }

    public void UlongBinaryOperatorExpressionVariationsHigh(ulong input)
    {
        var memory = CreateMemory(96);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        UlongBinaryOperatorExpressionVariationsHigh(memory);
    }

    public virtual void UlongBinaryOperatorExpressionVariationsHigh(SimpleMemory memory)
    {
        ulong input = ((ulong)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);
        var ulongLeft = input;

        var intRight = (int)input;
        SaveResult(memory, 48, (long)(ulongLeft << intRight));
        SaveResult(memory, 50, (long)(ulongLeft >> intRight));

        var uintRight = (uint)input;
        SaveResult(memory, 52, (long)(ulongLeft << (int)uintRight));
        SaveResult(memory, 54, (long)(ulongLeft >> (int)uintRight));
        SaveResult(
            memory,
            56,
            (long)(ulongLeft + uintRight), // 56
            (long)(ulongLeft - uintRight), // 58
            (long)(ulongLeft * uintRight), // 60
            (long)(ulongLeft / uintRight), // 62
            (long)(ulongLeft % uintRight), // 64
            (long)(ulongLeft & uintRight), // 66
            (long)(ulongLeft | uintRight), // 68
            (long)(ulongLeft ^ uintRight)); // 70

        var longRight = (long)input;
        SaveResult(memory, 72, (long)(ulongLeft << (int)longRight));
        SaveResult(memory, 74, (long)(ulongLeft >> (int)longRight));

        var ulongRight = input;
        SaveResult(memory, 76, (long)(ulongLeft << (int)ulongRight));
        SaveResult(memory, 78, (long)(ulongLeft >> (int)ulongRight));
        SaveResult(
            memory,
            80,
            (long)(ulongLeft + ulongRight), // 80
            (long)(ulongLeft - ulongRight), // 82
            (long)(ulongLeft * ulongRight), // 84
            (long)(ulongLeft / ulongRight), // 86
            (long)(ulongLeft % ulongRight), // 88
            (long)(ulongLeft & ulongRight), // 90
            (long)(ulongLeft | ulongRight), // 92
            (long)(ulongLeft ^ ulongRight)); // 94
    }

#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand

    public virtual void AllUnaryOperatorExpressionVariations(SimpleMemory memory)
    {
        long input = ((long)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);

        var byteOperand = (byte)input;
        SaveResult(memory, 0, ~byteOperand);
        SaveResult(memory, 2, +byteOperand);
        SaveResult(memory, 4, -byteOperand);

        var sbyteOperand = (sbyte)input;
        SaveResult(memory, 6, ~sbyteOperand);
        SaveResult(memory, 8, +sbyteOperand);
        SaveResult(memory, 10, -sbyteOperand);

        var shortOperand = (short)input;
        SaveResult(memory, 12, ~shortOperand);
        SaveResult(memory, 14, +shortOperand);
        SaveResult(memory, 16, -shortOperand);

        var ushortOperand = (ushort)input;
        SaveResult(memory, 18, ~ushortOperand);
        SaveResult(memory, 20, +ushortOperand);
        SaveResult(memory, 22, -ushortOperand);

        var intOperand = (int)input;
        SaveResult(memory, 24, ~intOperand);
        SaveResult(memory, 26, +intOperand);
        SaveResult(memory, 28, -intOperand);

        var uintOperand = (uint)input;
        SaveResult(memory, 30, ~uintOperand);
        SaveResult(memory, 32, +uintOperand);
        SaveResult(memory, 34, -uintOperand);

        var longOperand = input;
        SaveResult(memory, 36, ~longOperand);
        SaveResult(memory, 38, +longOperand);
        SaveResult(memory, 40, -longOperand);

        var ulongOperand = (ulong)input;
        SaveResult(memory, 42, (long)~ulongOperand);
        SaveResult(memory, 44, (long)+ulongOperand);
    }

    public void AllUnaryOperatorExpressionVariations(long input)
    {
        var memory = CreateMemory(46);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        AllUnaryOperatorExpressionVariations(memory);
    }

    private void SaveResult(
        SimpleMemory memory,
        int startCellIndex,
        long number0,
        long number1,
        long number2,
        long number3,
        long number4,
        long number5,
        long number6,
        long number7)
    {
        SaveResult(memory, startCellIndex, number0);
        SaveResult(memory, startCellIndex + 2, number1);
        SaveResult(memory, startCellIndex + 4, number2);
        SaveResult(memory, startCellIndex + 6, number3);
        SaveResult(memory, startCellIndex + 8, number4);
        SaveResult(memory, startCellIndex + 10, number5);
        SaveResult(memory, startCellIndex + 12, number6);
        SaveResult(memory, startCellIndex + 14, number7);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Static methods are not supported.")]
    private void SaveResult(SimpleMemory memory, int startCellIndex, long number)
    {
        memory.WriteInt32(startCellIndex, (int)(number >> 32));
        memory.WriteInt32(startCellIndex + 1, (int)number);
    }
}
