using Hast.Samples.SampleAssembly;
using System;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositComparisonTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void HardwareEntryPointsReturnIndependentComparisonFlags(int size)
    {
        var calculator = new PositComparisonCalculator();
        Action<Memory> compare = size switch
        {
            8 => calculator.ComparePosit8,
            16 => calculator.ComparePosit16,
            _ => calculator.ComparePosit32E2,
        };
        var memory = Memory.CreateSoftwareMemory(3);
        var mask = (uint)((1UL << size) - 1);
        var sign = 1U << (size - 1);
        if (size == 8)
        {
            for (uint left = 0; left <= mask; left++)
            {
                for (uint right = 0; right <= mask; right++) Check(left, right);
            }
        }
        else
        {
            uint[] boundaries = [0, 1, 2, 3, sign - 3, sign - 2, sign - 1, sign, sign + 1, sign + 2, mask - 2, mask - 1, mask];
            foreach (var left in boundaries)
            {
                foreach (var right in boundaries) Check(left, right);
            }

            var random = new Random(20220503);
            for (var i = 0; i < 8192; i++) Check((uint)random.NextInt64((long)mask + 1), (uint)random.NextInt64((long)mask + 1));
        }

        void Check(uint left, uint right)
        {
            var signedLeft = left >= sign ? (long)left - (1L << size) : left;
            var signedRight = right >= sign ? (long)right - (1L << size) : right;
            var expected = signedLeft == signedRight ? 41 : signedLeft > signedRight ? 14 : 50;
            memory.WriteUInt32(0, left);
            memory.WriteUInt32(1, right);
            compare(memory);
            Assert.Equal(expected, memory.ReadInt32(2));
            if (size == 32)
            {
                calculator.ComparePosit32(memory);
                Assert.Equal(expected, memory.ReadInt32(2));
            }
        }
    }
}
