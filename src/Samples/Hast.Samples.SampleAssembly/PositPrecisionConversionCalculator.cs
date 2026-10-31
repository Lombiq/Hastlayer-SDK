using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Reads the raw source encoding from cell 0. For conversions involving 32-bit values, cells 1 and 2 contain the
/// Posit32E2 and Posit32 compatibility results respectively; other conversions write cell 1.
/// </summary>
public class PositPrecisionConversionCalculator
{
    public virtual void ConvertPosit8To16(SimpleMemory memory)
    {
        var source = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit16E2)source).PositBits);
    }

    public virtual void ConvertPosit8To32(SimpleMemory memory)
    {
        var source = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit32E2)source).PositBits);
        memory.WriteUInt32(2, ((Posit32)source).PositBits);
    }

    public virtual void ConvertPosit16To8(SimpleMemory memory)
    {
        var source = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit8E2)source).PositBits);
    }

    public virtual void ConvertPosit16To32(SimpleMemory memory)
    {
        var source = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit32E2)source).PositBits);
        memory.WriteUInt32(2, ((Posit32)source).PositBits);
    }

    public virtual void ConvertPosit32To8(SimpleMemory memory)
    {
        var sourceE2 = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        var sourceCompatibility = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit8E2)sourceE2).PositBits);
        memory.WriteUInt32(2, ((Posit8E2)sourceCompatibility).PositBits);
    }

    public virtual void ConvertPosit32To16(SimpleMemory memory)
    {
        var sourceE2 = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        var sourceCompatibility = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, ((Posit16E2)sourceE2).PositBits);
        memory.WriteUInt32(2, ((Posit16E2)sourceCompatibility).PositBits);
    }
}
