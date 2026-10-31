using Hast.Layer;
using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;

namespace Hast.Samples.Posit.Extensions;

public static class Posit16E4CalculatorCalculatorExtensions
{
    public static int CalculateIntegerSumUpToNumber(
        this Posit16E4Calculator positCalculator,
        int number,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);

        memory.WriteInt32(Posit16E4Calculator.CalculateLargeIntegerSumInputInt32Index, number);
        positCalculator.CalculateIntegerSumUpToNumber(memory);

        return memory.ReadInt32(Posit16E4Calculator.CalculateLargeIntegerSumOutputInt32Index);
    }

    public static float CalculatePowerOfReal(
        this Posit16E4Calculator positCalculator,
        int number,
        float real,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(2)
            : hastlayer.CreateMemory(configuration, 2);

        memory.WriteInt32(Posit16E4Calculator.CalculatePowerOfRealInputInt32Index, number);
        memory.WriteUInt32(Posit16E4Calculator.CalculatePowerOfRealInputPosit32Index, new Posit16E4(real).PositBits);

        positCalculator.CalculatePowerOfReal(memory);

        return (float)new Posit16E4(
            (ushort)memory.ReadUInt32(Posit16E4Calculator.CalculatePowerOfRealOutputPosit32Index),
            fromBitMask: true);
    }

    public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(
        this Posit16E4Calculator positCalculator,
        int[] numbers,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        if (numbers.Length != Posit16E4Calculator.MaxDegreeOfParallelism)
        {
            throw new ArgumentException(StringHelper.CreateInvariant(
                $"Provide as many {nameof(numbers)} as the degree of parallelism of Posit16E4Calculator is " +
                $"({Posit16E4Calculator.MaxDegreeOfParallelism})"));
        }

        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(Posit16E4Calculator.MaxDegreeOfParallelism)
            : hastlayer.CreateMemory(configuration, Posit16E4Calculator.MaxDegreeOfParallelism);

        for (int i = 0; i < numbers.Length; i++)
        {
            memory.WriteInt32(Posit16E4Calculator.ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i, numbers[i]);
        }

        positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

        var results = new int[Posit16E4Calculator.MaxDegreeOfParallelism];

        for (int i = 0; i < numbers.Length; i++)
        {
            results[i] = memory.ReadInt32(Posit16E4Calculator.ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i);
        }

        return results;
    }

    public static float AddPositsInArray(
        this Posit16E4Calculator positCalculator,
        uint[] positArray,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(positArray.Length + 1)
            : hastlayer.CreateMemory(configuration, positArray.Length + 1);

        memory.WriteUInt32(Posit16E4Calculator.AddPositsInArrayInputPosit32CountIndex, (uint)positArray.Length);

        for (var i = 0; i < positArray.Length; i++)
        {
            memory.WriteUInt32(Posit16E4Calculator.AddPositsInArrayInputPosit32sStartIndex + i, positArray[i]);
        }

        positCalculator.AddPositsInArray(memory);

        return (float)new Posit32(memory.ReadUInt32(Posit16E4Calculator.AddPositsInArrayOutputPosit32Index), fromBitMask: true);
    }
}
