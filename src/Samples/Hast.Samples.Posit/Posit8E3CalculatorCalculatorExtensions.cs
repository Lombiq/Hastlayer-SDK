using Hast.Layer;
using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;

namespace Hast.Samples.Posit.Extensions;

public static class Posit8E3CalculatorCalculatorExtensions
{
    public static int CalculateIntegerSumUpToNumber(
        this Posit8E3Calculator positCalculator,
        int number,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);

        memory.WriteInt32(Posit8E3Calculator.CalculateLargeIntegerSumInputInt32Index, number);
        positCalculator.CalculateIntegerSumUpToNumber(memory);

        return memory.ReadInt32(Posit8E3Calculator.CalculateLargeIntegerSumOutputInt32Index);
    }

    public static float CalculatePowerOfReal(
        this Posit8E3Calculator positCalculator,
        int number,
        float real,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(2)
            : hastlayer.CreateMemory(configuration, 2);

        memory.WriteInt32(Posit8E3Calculator.CalculatePowerOfRealInputInt32Index, number);
        memory.WriteUInt32(Posit8E3Calculator.CalculatePowerOfRealInputPosit32Index, new Posit8E3(real).PositBits);

        positCalculator.CalculatePowerOfReal(memory);

        return (float)new Posit8E3(
            (byte)memory.ReadUInt32(Posit8E3Calculator.CalculatePowerOfRealOutputPosit32Index),
            fromBitMask: true);
    }

    public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(
        this Posit8E3Calculator positCalculator,
        int[] numbers,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        if (numbers.Length != Posit8E3Calculator.MaxDegreeOfParallelism)
        {
            throw new ArgumentException(StringHelper.CreateInvariant(
                $"Provide as many {nameof(numbers)} as the degree of parallelism of Posit8E3Calculator is " +
                $"({Posit8E3Calculator.MaxDegreeOfParallelism})"));
        }

        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(Posit8E3Calculator.MaxDegreeOfParallelism)
            : hastlayer.CreateMemory(configuration, Posit8E3Calculator.MaxDegreeOfParallelism);

        for (int i = 0; i < numbers.Length; i++)
        {
            memory.WriteInt32(Posit8E3Calculator.ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i, numbers[i]);
        }

        positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

        var results = new int[Posit8E3Calculator.MaxDegreeOfParallelism];

        for (int i = 0; i < numbers.Length; i++)
        {
            results[i] = memory.ReadInt32(Posit8E3Calculator.ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i);
        }

        return results;
    }

    public static float AddPositsInArray(
        this Posit8E3Calculator positCalculator,
        uint[] positArray,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(positArray.Length + 1)
            : hastlayer.CreateMemory(configuration, positArray.Length + 1);

        memory.WriteUInt32(Posit8E3Calculator.AddPositsInArrayInputPosit32CountIndex, (uint)positArray.Length);

        for (var i = 0; i < positArray.Length; i++)
        {
            memory.WriteUInt32(Posit8E3Calculator.AddPositsInArrayInputPosit32sStartIndex + i, positArray[i]);
        }

        positCalculator.AddPositsInArray(memory);

        return (float)new Posit32(memory.ReadUInt32(Posit8E3Calculator.AddPositsInArrayOutputPosit32Index), fromBitMask: true);
    }
}
