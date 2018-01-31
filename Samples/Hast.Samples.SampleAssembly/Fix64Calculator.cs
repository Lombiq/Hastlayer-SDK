using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Algorithms;
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
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;
        public const int ParallelizedCalculateLargeIntegerSum_InputInt32Index = 0;
        public const int ParallelizedCalculateLargeIntegerSum_OutputInt32Index = 0;

        public const int MaxDegreeOfParallelism = 10;


        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadInt32(CalculateLargeIntegerSum_InputInt32Index);

            var a = new Fix64(1);
            var b = a;

            for (var i = 1; i < number; i++)
            {
                a += b;
            }

            var integers = a.ToIntegers();
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, integers[0]);
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index + 1, integers[1]);
        }

        public virtual void ParallelizedCalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadInt32(ParallelizedCalculateLargeIntegerSum_InputInt32Index);

            var tasks = new Task<TaskResult>[MaxDegreeOfParallelism];

            for (uint i = 0; i < MaxDegreeOfParallelism; i++)
            {
                tasks[i] = Task.Factory.StartNew(
                    indexObject =>
                    {
                        var a = new Fix64(1);
                        var b = a;

                        for (var j = 1; j < number; j++)
                        {
                            a += b;
                        }

                        var integers = a.ToIntegers();

                        return new TaskResult
                        {
                            Fix64Low = integers[0],
                            Fix64High = integers[1]
                        };
                    }, i);
            }

            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                var itemOutputStartIndex = ParallelizedCalculateLargeIntegerSum_OutputInt32Index + i * 2;

                memory.WriteInt32(itemOutputStartIndex, tasks[i].Result.Fix64Low);
                memory.WriteInt32(itemOutputStartIndex + 1, tasks[i].Result.Fix64High);
            }
        }


        private class TaskResult
        {
            public int Fix64Low { get; set; }
            public int Fix64High { get; set; }
        }
    }


    public static class Fix64CalculatorExtensions
    {
        public static Fix64 CalculateIntegerSumUpToNumber(this Fix64Calculator algorithm, int input)
        {
            var memory = new SimpleMemory(2);

            memory.WriteInt32(Fix64Calculator.CalculateLargeIntegerSum_InputInt32Index, input);

            algorithm.CalculateIntegerSumUpToNumber(memory);

            return Fix64.FromRawInts(new[]
            {
                memory.ReadInt32(Fix64Calculator.CalculateLargeIntegerSum_OutputInt32Index),
                memory.ReadInt32(Fix64Calculator.CalculateLargeIntegerSum_OutputInt32Index + 1)
            });
        }

        public static IEnumerable<Fix64> ParallelizedCalculateIntegerSumUpToNumber(this Fix64Calculator algorithm, int input)
        {
            var memory = new SimpleMemory(2 * Fix64Calculator.MaxDegreeOfParallelism);

            memory.WriteInt32(Fix64Calculator.ParallelizedCalculateLargeIntegerSum_InputInt32Index, input);

            algorithm.ParallelizedCalculateIntegerSumUpToNumber(memory);

            var results = new Fix64[Fix64Calculator.MaxDegreeOfParallelism];

            for (int i = 0; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                var itemOutputStartIndex = Fix64Calculator.ParallelizedCalculateLargeIntegerSum_OutputInt32Index + i * 2;

                results[i] = Fix64.FromRawInts(new[]
                {
                    memory.ReadInt32(itemOutputStartIndex),
                    memory.ReadInt32(itemOutputStartIndex + 1)
                });
            }

            return results;
        }
    }
}
