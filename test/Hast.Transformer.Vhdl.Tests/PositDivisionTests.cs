using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics.Tests;
using System;
using System.Globalization;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositDivisionTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void HardwareEntryPointsMatchIndependentExactQuotients(int size)
    {
        var calculator = new PositDivisionCalculator();
        var memory = Memory.CreateSoftwareMemory(3);
        Action<Memory> divide = size switch
        {
            8 => calculator.DividePosit8,
            16 => calculator.DividePosit16,
            _ => calculator.DividePosit32E2,
        };

        foreach (var (left, right) in PositDivisionReference.Pairs(size))
        {
            var expected = PositDivisionReference.Divide(left, right, size);
            memory.WriteUInt32(0, left);
            memory.WriteUInt32(1, right);
            divide(memory);
            Check();
            if (size != 32) continue;

            calculator.DividePosit32(memory);
            Check();

            void Check()
            {
                Assert.Equal(left, memory.ReadUInt32(0));
                Assert.Equal(right, memory.ReadUInt32(1));
                var actual = memory.ReadUInt32(2);
                Assert.True(
                    expected == actual,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Division memory cell 2, posit{size} 0x{left:X8} / 0x{right:X8}: expected 0x{expected:X8}, got 0x{actual:X8}."));
            }
        }
    }
}
