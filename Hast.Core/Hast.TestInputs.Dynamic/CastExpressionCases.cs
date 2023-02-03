using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.TestInputs.Dynamic;

public class CastExpressionCases : DynamicTestInputBase
{
    public virtual void AllNumberCastingVariations(SimpleMemory memory)
    {
        // Separate input for int so it doesn't need to be downcast from the long one which can potentially be wrong
        // too.
        var intInput = memory.ReadInt32(1);
        long longInput = ((long)memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);

        var ulongResult = (ulong)longInput;

        // Casting from long.
        memory.WriteInt32(0, (int)(ulongResult >> 32));
        memory.WriteInt32(1, (int)ulongResult);
        memory.WriteInt32(2, (int)longInput);
        memory.WriteUInt32(3, (uint)longInput);
        memory.WriteInt32(4, (short)longInput);
        memory.WriteUInt32(5, (ushort)longInput);
        memory.WriteInt32(6, (sbyte)longInput);
        memory.WriteUInt32(7, (byte)longInput);

        // Casting from ulong.
        var longResult = (long)ulongResult;
        memory.WriteInt32(8, (int)(longResult >> 32));
        memory.WriteInt32(9, (int)longResult);
        memory.WriteInt32(10, (int)ulongResult);
        memory.WriteUInt32(11, (uint)ulongResult);
        memory.WriteInt32(12, (short)ulongResult);
        memory.WriteUInt32(13, (ushort)ulongResult);
        memory.WriteInt32(14, (sbyte)ulongResult);
        memory.WriteUInt32(15, (byte)ulongResult);

        // Casting from int.
        longResult = intInput;
        memory.WriteInt32(16, (int)(longResult >> 32));
        memory.WriteInt32(17, (int)longResult);
        ulongResult = (ulong)intInput;
        memory.WriteInt32(18, (int)(ulongResult >> 32));
        memory.WriteInt32(19, (int)ulongResult);
        memory.WriteUInt32(20, (uint)intInput);
        memory.WriteInt32(21, (short)intInput);
        memory.WriteUInt32(22, (ushort)intInput);
        memory.WriteInt32(23, (sbyte)intInput);
        memory.WriteUInt32(24, (byte)intInput);

        // Casting from uint.
        var uintResult = (uint)intInput;
        longResult = uintResult;
        memory.WriteInt32(25, (int)(longResult >> 32));
        memory.WriteInt32(26, (int)longResult);
        ulongResult = uintResult;
        memory.WriteInt32(27, (int)(ulongResult >> 32));
        memory.WriteInt32(28, (int)ulongResult);
        memory.WriteInt32(29, (int)uintResult);
        memory.WriteInt32(30, (short)uintResult);
        memory.WriteUInt32(31, (ushort)uintResult);
        memory.WriteInt32(32, (sbyte)uintResult);
        memory.WriteUInt32(33, (byte)uintResult);

        // Casting from short.
        var shortResult = (short)intInput;
        longResult = shortResult;
        memory.WriteInt32(34, (int)(longResult >> 32));
        memory.WriteInt32(35, (int)longResult);
        ulongResult = (ulong)shortResult;
        memory.WriteInt32(36, (int)(ulongResult >> 32));
        memory.WriteInt32(37, (int)ulongResult);
        memory.WriteInt32(38, shortResult);
        memory.WriteUInt32(39, (uint)shortResult);
        memory.WriteUInt32(40, (ushort)shortResult);
        memory.WriteInt32(41, (sbyte)shortResult);
        memory.WriteUInt32(42, (byte)shortResult);

        // Casting from ushort.
        var ushortResult = (ushort)intInput;
        longResult = ushortResult;
        memory.WriteInt32(43, (int)(longResult >> 32));
        memory.WriteInt32(44, (int)longResult);
        ulongResult = ushortResult;
        memory.WriteInt32(45, (int)(ulongResult >> 32));
        memory.WriteInt32(46, (int)ulongResult);
        memory.WriteInt32(47, ushortResult);
        memory.WriteUInt32(48, ushortResult);
        memory.WriteInt32(49, (short)ushortResult);
        memory.WriteInt32(50, (sbyte)ushortResult);
        memory.WriteUInt32(51, (byte)ushortResult);

        // Casting from sbyte.
        var sbyteResult = (sbyte)intInput;
        longResult = sbyteResult;
        memory.WriteInt32(52, (int)(longResult >> 32));
        memory.WriteInt32(53, (int)longResult);
        ulongResult = (ulong)sbyteResult;
        memory.WriteInt32(54, (int)(ulongResult >> 32));
        memory.WriteInt32(55, (int)ulongResult);
        memory.WriteInt32(56, sbyteResult);
        memory.WriteUInt32(57, (uint)sbyteResult);
        // Without this assignment, just passing sbyteResult directly to WriteInt32 would lack any cast in .NET.
        shortResult = sbyteResult;
        memory.WriteInt32(58, shortResult);
        memory.WriteInt32(59, (ushort)sbyteResult);
        memory.WriteUInt32(60, (byte)sbyteResult);

        // Casting from byte.
        var byteResult = (byte)intInput;
        longResult = byteResult;
        memory.WriteInt32(61, (int)(longResult >> 32));
        memory.WriteInt32(62, (int)longResult);
        ulongResult = byteResult;
        memory.WriteInt32(63, (int)(ulongResult >> 32));
        memory.WriteInt32(64, (int)ulongResult);
        memory.WriteInt32(65, byteResult);
        memory.WriteUInt32(66, byteResult);
        // Without these assignments, just passing byteResult directly to WriteInt32 and WriteUInt32 would lack any cast
        // in .NET.
        shortResult = byteResult;
        memory.WriteInt32(67, shortResult);
        ushortResult = byteResult;
        memory.WriteUInt32(68, ushortResult);
        memory.WriteInt32(69, (sbyte)byteResult);
    }

    public void AllNumberCastingVariations(long input)
    {
        var memory = CreateMemory(70);
        memory.WriteInt32(0, (int)(input >> 32));
        memory.WriteInt32(1, (int)input);
        AllNumberCastingVariations(memory);
    }
}
