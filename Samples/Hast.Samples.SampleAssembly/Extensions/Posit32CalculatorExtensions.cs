using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;
using System;
using System.Collections.Generic;

namespace Hast.Samples.SampleAssembly.Extensions
{
    public static class Posit32CalculatorExtensions
    {
        public static int CalculateIntegerSumUpToNumber(this Posit32Calculator positCalculator, int number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(1)
                : hastlayer.CreateMemory(configuration, 1);

            memory.WriteInt32(Posit32Calculator.CalculateLargeIntegerSumInputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(Posit32Calculator.CalculateLargeIntegerSumOutputInt32Index);
        }

        public static float CalculatePowerOfReal(this Posit32Calculator positCalculator, int number, float real, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(2)
                : hastlayer.CreateMemory(configuration, 2);

            memory.WriteInt32(Posit32Calculator.CalculatePowerOfRealInputInt32Index, number);
            memory.WriteUInt32(Posit32Calculator.CalculatePowerOfRealInputPosit32Index, new Posit32(real).PositBits);

            positCalculator.CalculatePowerOfReal(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32Calculator.CalculatePowerOfRealOutputPosit32Index), true);
        }

        public static IEnumerable<int> ParallelizedCalculateIntegerSumUpToNumbers(
            this Posit32Calculator positCalculator,
            int[] numbers,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            if (numbers.Length != Posit32Calculator.MaxDegreeOfParallelism)
            {
                throw new ArgumentException(
                    "Provide as many " + nameof(numbers) + " as the degree of parallelism of Posit32Calculator is (" +
                    Posit32Calculator.MaxDegreeOfParallelism + ")");
            }

            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(Posit32Calculator.MaxDegreeOfParallelism)
                : hastlayer.CreateMemory(configuration, Posit32Calculator.MaxDegreeOfParallelism);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteInt32(Posit32Calculator.ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i, numbers[i]);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

            var results = new int[Posit32Calculator.MaxDegreeOfParallelism];

            for (int i = 0; i < numbers.Length; i++)
            {
                results[i] = memory.ReadInt32(Posit32Calculator.ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + i);
            }

            return results;
        }

        public static float AddPositsInArray(this Posit32Calculator posit32Calculator, uint[] posit32Array, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var cellCount = posit32Array.Length + 1;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(configuration, cellCount);

            memory.WriteUInt32(Posit32Calculator.AddPositsInArrayInputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32Calculator.AddPositsInArrayInputPosit32sStartIndex + i, posit32Array[i]);
            }

            posit32Calculator.AddPositsInArray(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32Calculator.AddPositsInArrayOutputPosit32Index), true);
        }
    }
}
