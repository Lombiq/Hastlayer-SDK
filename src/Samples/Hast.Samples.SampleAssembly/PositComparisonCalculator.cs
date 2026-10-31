using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Raw inputs occupy cells 0 and 1. Cell 2 contains flags for equality, inequality, greater, greater-or-equal,
/// less and less-or-equal (bits 0-5).
/// </summary>
public class PositComparisonCalculator
{
    public virtual void ComparePosit8(SimpleMemory memory)
    {
        var left = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit8E2((byte)memory.ReadUInt32(1), fromBitMask: true);
        var flags = (left == right ? 1 : 0) | (left != right ? 2 : 0) |
            (left > right ? 4 : 0) | (left >= right ? 8 : 0) |
            (left < right ? 16 : 0) | (left <= right ? 32 : 0);
        memory.WriteInt32(2, flags);
    }

    public virtual void ComparePosit16(SimpleMemory memory)
    {
        var left = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit16E2((ushort)memory.ReadUInt32(1), fromBitMask: true);
        var flags = (left == right ? 1 : 0) | (left != right ? 2 : 0) |
            (left > right ? 4 : 0) | (left >= right ? 8 : 0) |
            (left < right ? 16 : 0) | (left <= right ? 32 : 0);
        memory.WriteInt32(2, flags);
    }

    public virtual void ComparePosit32E2(SimpleMemory memory)
    {
        var left = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit32E2(memory.ReadUInt32(1), fromBitMask: true);
        var flags = (left == right ? 1 : 0) | (left != right ? 2 : 0) |
            (left > right ? 4 : 0) | (left >= right ? 8 : 0) |
            (left < right ? 16 : 0) | (left <= right ? 32 : 0);
        memory.WriteInt32(2, flags);
    }

    public virtual void ComparePosit32(SimpleMemory memory)
    {
        var left = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit32(memory.ReadUInt32(1), fromBitMask: true);
        var flags = (left == right ? 1 : 0) | (left != right ? 2 : 0) |
            (left > right ? 4 : 0) | (left >= right ? 8 : 0) |
            (left < right ? 16 : 0) | (left <= right ? 32 : 0);
        memory.WriteInt32(2, flags);
    }
}
