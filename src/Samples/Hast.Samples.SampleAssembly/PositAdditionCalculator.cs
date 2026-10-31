using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Reads raw posits a, b and c from cells 0-2 and writes a + b, a - b, (a + b) - c, a + (b - c) and (a - c) + b to
/// cells 3-7 respectively, rounding after every operator. All entry points use eight 32-bit cells; narrow encodings
/// occupy the low bits.
/// </summary>
public class PositAdditionCalculator
{
    public virtual void AddPosit8(SimpleMemory memory)
    {
        var a = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        var b = new Posit8E2((byte)memory.ReadUInt32(1), fromBitMask: true);
        var c = new Posit8E2((byte)memory.ReadUInt32(2), fromBitMask: true);
        memory.WriteUInt32(3, (a + b).PositBits);
        memory.WriteUInt32(4, (a - b).PositBits);
        memory.WriteUInt32(5, (a + b - c).PositBits);
        memory.WriteUInt32(6, (a + (b - c)).PositBits);
        memory.WriteUInt32(7, (a - c + b).PositBits);
    }

    public virtual void AddPosit16(SimpleMemory memory)
    {
        var a = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        var b = new Posit16E2((ushort)memory.ReadUInt32(1), fromBitMask: true);
        var c = new Posit16E2((ushort)memory.ReadUInt32(2), fromBitMask: true);
        memory.WriteUInt32(3, (a + b).PositBits);
        memory.WriteUInt32(4, (a - b).PositBits);
        memory.WriteUInt32(5, (a + b - c).PositBits);
        memory.WriteUInt32(6, (a + (b - c)).PositBits);
        memory.WriteUInt32(7, (a - c + b).PositBits);
    }

    public virtual void AddPosit32E2(SimpleMemory memory)
    {
        var a = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        var b = new Posit32E2(memory.ReadUInt32(1), fromBitMask: true);
        var c = new Posit32E2(memory.ReadUInt32(2), fromBitMask: true);
        memory.WriteUInt32(3, (a + b).PositBits);
        memory.WriteUInt32(4, (a - b).PositBits);
        memory.WriteUInt32(5, (a + b - c).PositBits);
        memory.WriteUInt32(6, (a + (b - c)).PositBits);
        memory.WriteUInt32(7, (a - c + b).PositBits);
    }

    public virtual void AddPosit32(SimpleMemory memory)
    {
        var a = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        var b = new Posit32(memory.ReadUInt32(1), fromBitMask: true);
        var c = new Posit32(memory.ReadUInt32(2), fromBitMask: true);
        memory.WriteUInt32(3, (a + b).PositBits);
        memory.WriteUInt32(4, (a - b).PositBits);
        memory.WriteUInt32(5, (a + b - c).PositBits);
        memory.WriteUInt32(6, (a + (b - c)).PositBits);
        memory.WriteUInt32(7, (a - c + b).PositBits);
    }
}
