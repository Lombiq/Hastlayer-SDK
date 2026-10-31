using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics.Tests;
using System;
using System.Globalization;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositAdditionTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void HardwareEntryPointsMatchIndependentExactSums(int size)
    {
        var calculator = new PositAdditionCalculator();
        var memory = Memory.CreateSoftwareMemory(8);
        Action<Memory> calculate = size switch
        {
            8 => calculator.AddPosit8,
            16 => calculator.AddPosit16,
            _ => calculator.AddPosit32E2,
        };

        // Every posit8 pair, hard and seeded wider pairs; c = a also checks absorption in (a + b) - a.
        foreach (var (left, right) in PositAdditionReference.Pairs(size)) Verify(left, right, left);
        foreach (var (a, b, c) in PositAdditionReference.RandomTriples(size)) Verify(a, b, c);
        foreach (var (a, b, c) in PositAdditionReference.DistinguishingTriples(size))
        {
            Verify(a, b, c);
            Assert.NotEqual(PositAdditionReference.FusedSum(a, b, c, size), memory.ReadUInt32(5));
        }

        void Verify(uint a, uint b, uint c)
        {
            var expressions = PositAdditionReference.EvaluateExpressions(a, b, c, size);
            uint[] expected =
            [
                PositAdditionReference.Add(a, b, size),
                PositAdditionReference.Subtract(a, b, size),
                expressions[0],
                expressions[1],
                expressions[2],
            ];

            Run(calculate);
            if (size == 32) Run(calculator.AddPosit32);

            void Run(Action<Memory> entryPoint)
            {
                memory.WriteUInt32(0, a);
                memory.WriteUInt32(1, b);
                memory.WriteUInt32(2, c);
                entryPoint(memory);
                Assert.Equal(a, memory.ReadUInt32(0));
                Assert.Equal(b, memory.ReadUInt32(1));
                Assert.Equal(c, memory.ReadUInt32(2));
                for (var output = 0; output < expected.Length; output++)
                {
                    var actual = memory.ReadUInt32(output + 3);
                    Assert.True(
                        expected[output] == actual,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Addition memory cell {output + 3}, posit{size} 0x{a:X8}, 0x{b:X8}, 0x{c:X8}: " +
                            $"expected 0x{expected[output]:X8}, got 0x{actual:X8}."));
                }
            }
        }
    }
}
