using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics.Tests;
using System;
using System.Globalization;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositUnaryTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void HardwareEntryPointsMatchIndependentExactUnaryResults(int size)
    {
        var calculator = new PositUnaryCalculator();
        var memory = Memory.CreateSoftwareMemory(9);
        Action<Memory> calculate = size switch
        {
            8 => calculator.UnaryPosit8,
            16 => calculator.UnaryPosit16,
            _ => calculator.UnaryPosit32E2,
        };

        foreach (var bits in PositUnaryReference.Inputs(size))
        {
            var expected = PositUnaryReference.Evaluate(bits, size);
            memory.WriteUInt32(0, bits);
            calculate(memory);
            Check();
            if (size != 32) continue;

            calculator.UnaryPosit32(memory);
            Check();

            void Check()
            {
                Assert.Equal(bits, memory.ReadUInt32(0));
                for (var operation = 0; operation < expected.Length; operation++)
                {
                    var actual = memory.ReadUInt32(operation + 1);
                    Assert.True(
                        expected[operation] == actual,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Unary memory cell {operation + 1}, posit{size} 0x{bits:X8}: expected 0x{expected[operation]:X8}, got 0x{actual:X8}."));
                }
            }
        }
    }
}
