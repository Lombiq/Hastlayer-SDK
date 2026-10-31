using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Reads raw posit dividend and divisor from cells 0 and 1 and writes their correctly rounded quotient to cell 2. All
/// entry points use three 32-bit cells; narrow encodings occupy the low bits.
/// </summary>
public class PositDivisionCalculator
{
    public virtual void DividePosit8(SimpleMemory memory) =>
        memory.WriteUInt32(
            2,
            (new Posit8E2((byte)memory.ReadUInt32(0), fromBitMask: true) /
                new Posit8E2((byte)memory.ReadUInt32(1), fromBitMask: true)).PositBits);

    public virtual void DividePosit16(SimpleMemory memory) =>
        memory.WriteUInt32(
            2,
            (new Posit16E2((ushort)memory.ReadUInt32(0), fromBitMask: true) /
                new Posit16E2((ushort)memory.ReadUInt32(1), fromBitMask: true)).PositBits);

    public virtual void DividePosit32E2(SimpleMemory memory) =>
        memory.WriteUInt32(
            2,
            (new Posit32E2(memory.ReadUInt32(0), fromBitMask: true) /
                new Posit32E2(memory.ReadUInt32(1), fromBitMask: true)).PositBits);

    public virtual void DividePosit32(SimpleMemory memory) =>
        memory.WriteUInt32(
            2,
            (new Posit32(memory.ReadUInt32(0), fromBitMask: true) /
                new Posit32(memory.ReadUInt32(1), fromBitMask: true)).PositBits);
}
