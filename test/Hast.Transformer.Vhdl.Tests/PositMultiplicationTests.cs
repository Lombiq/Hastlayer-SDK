using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

using Memory = Hast.Transformer.SimpleMemory.SimpleMemory;

namespace Hast.Transformer.Vhdl.Tests;

public class PositMultiplicationTests
{
    private const int MaximumReportedMismatches = 20;

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void HardwareEntryPointsMatchIndependentExactProducts(int size)
    {
        var calculator = new PositMultiplicationCalculator();
        var memory = Memory.CreateSoftwareMemory(3);
        Action<Memory>[] entryPoints = size switch
        {
            8 => [calculator.MultiplyPosit8],
            16 => [calculator.MultiplyPosit16],
            _ => [calculator.MultiplyPosit32E2, calculator.MultiplyPosit32],
        };

        var failures = new List<string>();
        foreach (var (left, right) in GetPairs(size))
        {
            var expected = PositMultiplicationReference.Multiply(left, right, size);
            foreach (var calculate in entryPoints)
            {
                memory.WriteUInt32(0, left);
                memory.WriteUInt32(1, right);
                memory.WriteUInt32(2, ~expected);
                calculate(memory);

                var actual = memory.ReadUInt32(2);
                var operandsPreserved = memory.ReadUInt32(0) == left && memory.ReadUInt32(1) == right;
                if ((actual == expected && operandsPreserved) || failures.Count >= MaximumReportedMismatches) continue;

                failures.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Posit{size}: 0x{left:X8} * 0x{right:X8} expected 0x{expected:X8}, got 0x{actual:X8}."));
            }
        }

        Assert.Empty(failures);
    }

    private static IEnumerable<(uint Left, uint Right)> GetPairs(int size)
    {
        foreach (var pair in PositMultiplicationReference.CorePairs(size)) yield return pair;
        if (size == 8) yield break;

        foreach (var pair in PositMultiplicationReference.SeededPairs(size, 4_000)) yield return pair;
        foreach (var pair in PositMultiplicationReference.SweepPairs(size, PositMultiplicationReference.PatternOperands(size)))
        {
            yield return pair;
        }

        foreach (var (left, right, _) in PositMultiplicationReference.TiePairs(size, 20))
        {
            yield return (left, right);
            yield return (PositMultiplicationReference.Negate(left, size), right);
        }
    }
}
