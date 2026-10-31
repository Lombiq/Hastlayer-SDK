using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Raw inputs occupy cells 0 and 1 and the raw product, rounded once as in section 5.4 of the Posit Standard (2022),
/// is written to cell 2. All entry points use three 32-bit cells; narrow encodings occupy the low bits.
/// </summary>
public class PositMultiplicationCalculator
{
    public virtual void MultiplyPosit8(SimpleMemory memory)
    {
        var left = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit8E2((byte)memory.ReadUInt32(1), fromBitMask: true);
        memory.WriteUInt32(2, (left * right).PositBits);
    }

    public virtual void MultiplyPosit16(SimpleMemory memory)
    {
        var left = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit16E2((ushort)memory.ReadUInt32(1), fromBitMask: true);
        memory.WriteUInt32(2, (left * right).PositBits);
    }

    public virtual void MultiplyPosit32E2(SimpleMemory memory)
    {
        var left = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit32E2(memory.ReadUInt32(1), fromBitMask: true);
        memory.WriteUInt32(2, (left * right).PositBits);
    }

    public virtual void MultiplyPosit32(SimpleMemory memory)
    {
        var left = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        var right = new Posit32(memory.ReadUInt32(1), fromBitMask: true);
        memory.WriteUInt32(2, (left * right).PositBits);
    }
}
