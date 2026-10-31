using Hast.Samples.SampleAssembly;
using System;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositPrecisionConversionTests
{
    [Fact]
    public void HardwareEntryPointsPreserveWideningAndPosit32Compatibility()
    {
        var calculator = new PositPrecisionConversionCalculator();
        var memory = Memory.CreateSoftwareMemory(3);

        foreach (var bits in new uint[] { 0, 1, 0x20, 0x40, 0x7F, 0x80, 0x81, 0xFF })
        {
            memory.WriteUInt32(0, bits);
            calculator.ConvertPosit8To16(memory);
            Assert.Equal(bits << 8, memory.ReadUInt32(1));

            calculator.ConvertPosit8To32(memory);
            Assert.Equal(bits << 24, memory.ReadUInt32(1));
            Assert.Equal(memory.ReadUInt32(1), memory.ReadUInt32(2));
        }

        foreach (var bits in new uint[] { 0, 1, 0x0100, 0x4000, 0x7FFF, 0x8000, 0x8001, 0xFFFF })
        {
            memory.WriteUInt32(0, bits);
            calculator.ConvertPosit16To32(memory);
            Assert.Equal(bits << 16, memory.ReadUInt32(1));
            Assert.Equal(memory.ReadUInt32(1), memory.ReadUInt32(2));
        }
    }

    [Fact]
    public void HardwareEntryPointsReturnHandCheckedNarrowingResults()
    {
        var calculator = new PositPrecisionConversionCalculator();
        var memory = Memory.CreateSoftwareMemory(3);
        Check(calculator.ConvertPosit16To8, 0x2080, 0x20, verifyCompatibilityResult: false);
        Check(calculator.ConvertPosit16To8, 0x1F80, 0x20, verifyCompatibilityResult: false);
        Check(calculator.ConvertPosit32To8, 0x1F800000, 0x20);
        Check(calculator.ConvertPosit32To8, 0x20800000, 0x20);
        Check(calculator.ConvertPosit32To8, 0x20800001, 0x21);
        Check(calculator.ConvertPosit32To16, 0x001F8000, 0x20);
        Check(calculator.ConvertPosit32To16, 0x00208000, 0x20);
        Check(calculator.ConvertPosit32To16, 0x00208001, 0x21);
        Check(calculator.ConvertPosit32To8, 0, 0);
        Check(calculator.ConvertPosit32To8, 0x80000000, 0x80);
        Check(calculator.ConvertPosit32To8, 1, 1);
        Check(calculator.ConvertPosit32To8, uint.MaxValue, 0xFF);
        Check(calculator.ConvertPosit32To8, 0x7FFFFFFF, 0x7F);
        Check(calculator.ConvertPosit32To8, 0x80000001, 0x81);
        Check(calculator.ConvertPosit32To8, 0x40000000, 0x40);
        Check(calculator.ConvertPosit32To8, 0xC0000000, 0xC0);
        Check(calculator.ConvertPosit32To16, 0, 0);
        Check(calculator.ConvertPosit32To16, 0x80000000, 0x8000);
        Check(calculator.ConvertPosit32To16, 1, 1);
        Check(calculator.ConvertPosit32To16, uint.MaxValue, 0xFFFF);
        Check(calculator.ConvertPosit32To16, 0x7FFFFFFF, 0x7FFF);
        Check(calculator.ConvertPosit32To16, 0x80000001, 0x8001);
        Check(calculator.ConvertPosit32To16, 0x40000000, 0x4000);
        Check(calculator.ConvertPosit32To16, 0xC0000000, 0xC000);

        void Check(Action<Memory> convert, uint sourceBits, uint expectedBits, bool verifyCompatibilityResult = true)
        {
            memory.WriteUInt32(0, sourceBits);
            convert(memory);
            Assert.Equal(expectedBits, memory.ReadUInt32(1));
            if (verifyCompatibilityResult) Assert.Equal(expectedBits, memory.ReadUInt32(2));
        }
    }
}
