using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Reads one raw posit from cell 0 and writes negate, abs, sign, nearestInt, ceiling, floor, next and prior to
/// cells 1-8 respectively. All entry points use nine 32-bit cells; narrow encodings occupy the low bits.
/// </summary>
public class PositUnaryCalculator
{
    public virtual void UnaryPosit8(SimpleMemory memory)
    {
        var input = new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, Posit8E2.Negate(input).PositBits);
        memory.WriteUInt32(2, Posit8E2.Abs(input).PositBits);
        memory.WriteUInt32(3, Posit8E2.Sign(input).PositBits);
        memory.WriteUInt32(4, Posit8E2.NearestInt(input).PositBits);
        memory.WriteUInt32(5, Posit8E2.Ceiling(input).PositBits);
        memory.WriteUInt32(6, Posit8E2.Floor(input).PositBits);
        memory.WriteUInt32(7, Posit8E2.Next(input).PositBits);
        memory.WriteUInt32(8, Posit8E2.Prior(input).PositBits);
    }

    public virtual void UnaryPosit16(SimpleMemory memory)
    {
        var input = new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, Posit16E2.Negate(input).PositBits);
        memory.WriteUInt32(2, Posit16E2.Abs(input).PositBits);
        memory.WriteUInt32(3, Posit16E2.Sign(input).PositBits);
        memory.WriteUInt32(4, Posit16E2.NearestInt(input).PositBits);
        memory.WriteUInt32(5, Posit16E2.Ceiling(input).PositBits);
        memory.WriteUInt32(6, Posit16E2.Floor(input).PositBits);
        memory.WriteUInt32(7, Posit16E2.Next(input).PositBits);
        memory.WriteUInt32(8, Posit16E2.Prior(input).PositBits);
    }

    public virtual void UnaryPosit32E2(SimpleMemory memory)
    {
        var input = new Posit32E2(memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, Posit32E2.Negate(input).PositBits);
        memory.WriteUInt32(2, Posit32E2.Abs(input).PositBits);
        memory.WriteUInt32(3, Posit32E2.Sign(input).PositBits);
        memory.WriteUInt32(4, Posit32E2.NearestInt(input).PositBits);
        memory.WriteUInt32(5, Posit32E2.Ceiling(input).PositBits);
        memory.WriteUInt32(6, Posit32E2.Floor(input).PositBits);
        memory.WriteUInt32(7, Posit32E2.Next(input).PositBits);
        memory.WriteUInt32(8, Posit32E2.Prior(input).PositBits);
    }

    public virtual void UnaryPosit32(SimpleMemory memory)
    {
        var input = new Posit32(memory.ReadUInt32(0), fromBitMask: true);
        memory.WriteUInt32(1, Posit32.Negate(input).PositBits);
        memory.WriteUInt32(2, Posit32.Abs(input).PositBits);
        memory.WriteUInt32(3, Posit32.Sign(input).PositBits);
        memory.WriteUInt32(4, Posit32.NearestInt(input).PositBits);
        memory.WriteUInt32(5, Posit32.Ceiling(input).PositBits);
        memory.WriteUInt32(6, Posit32.Floor(input).PositBits);
        memory.WriteUInt32(7, Posit32.Next(input).PositBits);
        memory.WriteUInt32(8, Posit32.Prior(input).PositBits);
    }
}
