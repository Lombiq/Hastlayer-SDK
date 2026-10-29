using Hast.Layer;
using Hast.Transformer.SimpleMemory;
using Lombiq.Arithmetics;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Samples.Posit;

public class Posit8E1Calculator
{
    public const int CalculateLargeIntegerSumInputInt32Index = 0;
    public const int CalculateLargeIntegerSumOutputInt32Index = 0;
    public const int ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex = 0;
    public const int ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex = 0;
    public const int AddPositsInArrayInputPosit32CountIndex = 0;
    public const int AddPositsInArrayInputPosit32sStartIndex = 1;
    public const int AddPositsInArrayOutputPosit32Index = 2;
    public const int CalculatePowerOfRealInputInt32Index = 0;
    public const int CalculatePowerOfRealInputPosit32Index = 1;
    public const int CalculatePowerOfRealOutputPosit32Index = 0;

    public const int MaxDegreeOfParallelism = 5;

    public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
    {
        var number = memory.ReadUInt32(CalculateLargeIntegerSumInputInt32Index);

        var a = new Posit8E1(1);
        var b = a;

        for (uint i = 1; i < number; i++)
        {
            a += b;
        }

        var result = (int)a;
        memory.WriteInt32(CalculateLargeIntegerSumOutputInt32Index, result);
    }

    public virtual void CalculatePowerOfReal(SimpleMemory memory)
    {
        var number = memory.ReadInt32(CalculatePowerOfRealInputInt32Index);
        var positToMultiply = (byte)memory.ReadUInt32(CalculatePowerOfRealInputPosit32Index);

        var a = new Posit8E1(positToMultiply, fromBitMask: true);
        var b = a;

        for (uint i = 0; i < number; i++)
        {
            a *= b;
        }

        var result = a.PositBits;
        memory.WriteUInt32(CalculatePowerOfRealOutputPosit32Index, result);
    }

    public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
    {
        var tasks = new Task<int>[MaxDegreeOfParallelism];

        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i);

            tasks[i] = Task.Factory.StartNew(
                upToNumberObject =>
                {
                    var a = new Posit8E1(1);
                    var b = a;

                    for (int j = 1; j < (int)upToNumberObject; j++)
                    {
                        a += b;
                    }

                    return (int)a;
                },
                upToNumber);
        }

        Task.WhenAll(tasks).Wait();

        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            memory.WriteInt32(ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i, tasks[i].Result);
        }
    }

    public virtual void AddPositsInArray(SimpleMemory memory)
    {
        uint numberCount = memory.ReadUInt32(AddPositsInArrayInputPosit32CountIndex);

        var result = new Posit8E1(
            (byte)memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex),
            fromBitMask: true);

        for (int i = 1; i < numberCount; i++)
        {
            result += new Posit8E1(
                (byte)memory.ReadUInt32(AddPositsInArrayInputPosit32sStartIndex + i),
                fromBitMask: true);
        }

        memory.WriteUInt32(AddPositsInArrayOutputPosit32Index, result.PositBits);
    }
}

public static class Posit8E1CalculatorCalculatorExtensions
{
    public static int CalculateIntegerSumUpToNumber(
        this Posit8E1Calculator positCalculator,
        int number,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);

        memory.WriteInt32(Posit8E1Calculator.CalculateLargeIntegerSumInputInt32Index, number);
        positCalculator.CalculateIntegerSumUpToNumber(memory);

        return memory.ReadInt32(Posit8E1Calculator.CalculateLargeIntegerSumOutputInt32Index);
    }

    public static float CalculatePowerOfReal(
        this Posit8E1Calculator positCalculator,
        int number,
        float real,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(2)
            : hastlayer.CreateMemory(configuration, 2);

        memory.WriteInt32(Posit8E1Calculator.CalculatePowerOfRealInputInt32Index, number);
        memory.WriteUInt32(Posit8E1Calculator.CalculatePowerOfRealInputPosit32Index, new Posit8E1(real).PositBits);

        positCalculator.CalculatePowerOfReal(memory);

        return (float)new Posit8E1(
            (byte)memory.ReadUInt32(Posit8E1Calculator.CalculatePowerOfRealOutputPosit32Index),
            fromBitMask: true);
    }

    public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(
        this Posit8E1Calculator positCalculator,
        int[] numbers,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        if (numbers.Length != Posit8E1Calculator.MaxDegreeOfParallelism)
        {
            throw new ArgumentException(StringHelper.CreateInvariant(
                $"Provide as many {nameof(numbers)} as the degree of parallelism of Posit8E1Calculator is " +
                $"({Posit8E1Calculator.MaxDegreeOfParallelism})"));
        }

        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(Posit8E1Calculator.MaxDegreeOfParallelism)
            : hastlayer.CreateMemory(configuration, Posit8E1Calculator.MaxDegreeOfParallelism);

        for (int i = 0; i < numbers.Length; i++)
        {
            memory.WriteInt32(Posit8E1Calculator.ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i, numbers[i]);
        }

        positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

        var results = new int[Posit8E1Calculator.MaxDegreeOfParallelism];

        for (int i = 0; i < numbers.Length; i++)
        {
            results[i] = memory.ReadInt32(Posit8E1Calculator.ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i);
        }

        return results;
    }

    public static float AddPositsInArray(
        this Posit8E1Calculator positCalculator,
        uint[] positArray,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(positArray.Length + 1)
            : hastlayer.CreateMemory(configuration, positArray.Length + 1);

        memory.WriteUInt32(Posit8E1Calculator.AddPositsInArrayInputPosit32CountIndex, (uint)positArray.Length);

        for (var i = 0; i < positArray.Length; i++)
        {
            memory.WriteUInt32(Posit8E1Calculator.AddPositsInArrayInputPosit32sStartIndex + i, positArray[i]);
        }

        positCalculator.AddPositsInArray(memory);

        return (float)new Posit32(memory.ReadUInt32(Posit8E1Calculator.AddPositsInArrayOutputPosit32Index), fromBitMask: true);
    }
}
