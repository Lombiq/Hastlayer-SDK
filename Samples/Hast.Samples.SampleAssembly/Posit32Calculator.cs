using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample showing how floating point numbers of type posit (<see href="https://posithub.org" />) can be used
    /// with Hastlayer. Using the statically-typed <see cref="Posit32"/> variant here.
    /// </summary>
    public class Posit32Calculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;
        public const int ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex = 0;
        public const int ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex = 0;
        public const int AddPositsInArray_InputPosit32CountIndex = 0;
        public const int AddPositsInArray_InputPosit32sStartIndex = 1;
        public const int AddPositsInArray_OutputPosit32Index = 2;
        public const int CalculatePowerOfReal_InputInt32Index = 0;
        public const int CalculatePowerOfReal_InputPosit32Index = 1;
        public const int CalculatePowerOfReal_OutputPosit32Index = 0;

        // This takes about 75% of a Nexys 4 DDR's FPGA. If only ParallelizedCalculateIntegerSumUpToNumbers is
        // selected as the hardware entry point (i.e. only it will be transformed into hardware, see the config in
        // Posit32CalculatorSampleRunner) then with a MaxDegreeOfParallelism of 5 it'll take 75% as well.
        public const int MaxDegreeOfParallelism = 2;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSum_InputInt32Index);

            var a = new Posit32(1);
            var b = a;

            for (uint i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, result);
        }

        public virtual void CalculatePowerOfReal(SimpleMemory memory)
        {
            var number = memory.ReadInt32(CalculatePowerOfReal_InputInt32Index);
            var positToMultiply = memory.ReadUInt32(CalculatePowerOfReal_InputPosit32Index);

            var a = new Posit32(positToMultiply, true);
            var b = a;

            for (uint i = 0; i < number; i++)
            {
                a *= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(CalculatePowerOfReal_OutputPosit32Index, result);
        }

        public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
        {
            var numbers = new int[MaxDegreeOfParallelism];

            var tasks = new Task<int>[MaxDegreeOfParallelism];

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex + i);

                tasks[i] = Task.Factory.StartNew(
                    upToNumberObject =>
                    {
                        var a = new Posit32(1);
                        var b = a;

                        for (int j = 1; j < (int)upToNumberObject; j++)
                        {
                            a += b;
                        }

                        return (int)a;
                    }, upToNumber);
            }

            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                memory.WriteInt32(ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex + i, tasks[i].Result);
            }
        }

        public virtual void AddPositsInArray(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(AddPositsInArray_InputPosit32CountIndex);

            var result = new Posit32(memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex), true);

            for (int i = 1; i < numberCount; i++)
            {
                result += new Posit32(memory.ReadUInt32(AddPositsInArray_InputPosit32sStartIndex + i), true);
            }

            memory.WriteUInt32(AddPositsInArray_OutputPosit32Index, result.PositBits);
        }
    }

    public static class Posit32CalculatorExtensions
    {
        public static int CalculateIntegerSumUpToNumber(this Posit32Calculator positCalculator, int number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(1)
                : hastlayer.CreateMemory(configuration, 1);

            memory.WriteInt32(Posit32Calculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(Posit32Calculator.CalculateLargeIntegerSum_OutputInt32Index);
        }

        public static float CalculatePowerOfReal(this Posit32Calculator positCalculator, int number, float real, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(2)
                : hastlayer.CreateMemory(configuration, 2);

            memory.WriteInt32(Posit32Calculator.CalculatePowerOfReal_InputInt32Index, number);
            memory.WriteUInt32(Posit32Calculator.CalculatePowerOfReal_InputPosit32Index, new Posit32(real).PositBits);

            positCalculator.CalculatePowerOfReal(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32Calculator.CalculatePowerOfReal_OutputPosit32Index), true);
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
                    "Provide as many numbers as the degree of parallelism of Posit32Calculator is (" +
                    Posit32Calculator.MaxDegreeOfParallelism + ")");
            }

            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(Posit32Calculator.MaxDegreeOfParallelism)
                : hastlayer.CreateMemory(configuration, Posit32Calculator.MaxDegreeOfParallelism);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteInt32(Posit32Calculator.ParallelizedCalculateLargeIntegerSum_Int32NumbersStartIndex + i, numbers[i]);
            }

            positCalculator.ParallelizedCalculateIntegerSumUpToNumbers(memory);

            var results = new int[Posit32Calculator.MaxDegreeOfParallelism];

            for (int i = 0; i < numbers.Length; i++)
            {
                results[i] = memory.ReadInt32(Posit32Calculator.ParallelizedCalculateLargeIntegerSum_OutputInt32sStartIndex + i);
            }

            return results;
        }

        public static float AddPositsInArray(this Posit32Calculator posit32Calculator, uint[] posit32Array, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var cellCount = posit32Array.Length + 1;
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(cellCount)
                : hastlayer.CreateMemory(configuration, cellCount);

            memory.WriteUInt32(Posit32Calculator.AddPositsInArray_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32Calculator.AddPositsInArray_InputPosit32sStartIndex + i, posit32Array[i]);
            }

            posit32Calculator.AddPositsInArray(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32Calculator.AddPositsInArray_OutputPosit32Index), true);
        }
    }
}
