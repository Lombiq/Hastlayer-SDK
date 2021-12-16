using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Algorithms;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Sample using the <see cref="Fix64"/> 64 fixed-point number type. This is useful if you need more involved
    /// calculations with fractions where simply scaling the numbers up and down is not enough. Also see
    /// <see cref="Fix64CalculatorSampleRunner"/> on what to configure to make this work.
    /// </summary>
    public class Fix64Calculator
    {
        private const int CalculateLargeIntegerSumInputInt32Index = 0;
        private const int CalculateLargeIntegerSumOutputInt32Index = 0;
        private const int ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex = 0;
        private const int ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex = 0;

        public const int MaxDegreeOfParallelism = 10;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadInt32(CalculateLargeIntegerSumInputInt32Index);

            var a = new Fix64(1);
            var b = a;

            for (var i = 1; i < number; i++)
            {
                a += b;
            }

            var integers = a.ToIntegers();
            memory.WriteInt32(CalculateLargeIntegerSumOutputInt32Index, integers[0]);
            memory.WriteInt32(CalculateLargeIntegerSumOutputInt32Index + 1, integers[1]);
        }

        public virtual void ParallelizedCalculateIntegerSumUpToNumbers(SimpleMemory memory)
        {
            var numbers = new int[MaxDegreeOfParallelism];

            var tasks = new Task<TaskResult>[MaxDegreeOfParallelism];

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var upToNumber = memory.ReadInt32(ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i);

                tasks[i] = Task.Factory.StartNew(
                    upToNumberObject =>
                    {
                        var a = new Fix64(1);
                        var b = a;

                        for (var j = 1; j < (int)upToNumberObject; j++)
                        {
                            a += b;
                        }

                        var integers = a.ToIntegers();

                        return new TaskResult
                        {
                            Fix64Low = integers[0],
                            Fix64High = integers[1],
                        };
                    },
                    upToNumber);
            }

            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var itemOutputStartIndex = ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + (i * 2);

                memory.WriteInt32(itemOutputStartIndex, tasks[i].Result.Fix64Low);
                memory.WriteInt32(itemOutputStartIndex + 1, tasks[i].Result.Fix64High);
            }
        }

        private class TaskResult
        {
            public int Fix64Low { get; set; }
            public int Fix64High { get; set; }
        }

        public Fix64 CalculateIntegerSumUpToNumber(int input, IHastlayer hastlayer, IHardwareGenerationConfiguration configuration)
        {
            var memory = hastlayer.CreateMemory(configuration, 2);

            memory.WriteInt32(CalculateLargeIntegerSumInputInt32Index, input);

            CalculateIntegerSumUpToNumber(memory);

            return Fix64.FromRawInts(new[]
            {
                memory.ReadInt32(CalculateLargeIntegerSumOutputInt32Index),
                memory.ReadInt32(CalculateLargeIntegerSumOutputInt32Index + 1),
            });
        }

        public IEnumerable<Fix64> ParallelizedCalculateIntegerSumUpToNumbers(
            int[] numbers,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            if (numbers.Length != MaxDegreeOfParallelism)
            {
                throw new ArgumentException(
                    "Provide as many numbers as the degree of parallelism of Fix64Calculator is (" +
                    MaxDegreeOfParallelism + ")");
            }

            var memory = hastlayer is null ?
                SimpleMemory.CreateSoftwareMemory(2 * MaxDegreeOfParallelism) :
                hastlayer.CreateMemory(configuration, 2 * MaxDegreeOfParallelism);

            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteInt32(ParallelizedCalculateLargeIntegerSumInt32NumbersStartIndex + i, numbers[i]);
            }

            ParallelizedCalculateIntegerSumUpToNumbers(memory);

            var results = new Fix64[MaxDegreeOfParallelism];

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var itemOutputStartIndex = ParallelizedCalculateLargeIntegerSumOutputInt32sStartIndex + (i * 2);

                results[i] = Fix64.FromRawInts(new[]
                {
                    memory.ReadInt32(itemOutputStartIndex),
                    memory.ReadInt32(itemOutputStartIndex + 1),
                });
            }

            return results;
        }
    }
}
