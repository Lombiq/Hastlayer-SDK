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
    /// calculations with fractions where simply scaling the numbers up and down is not enough.
    /// </summary>
    public class Fix64Calculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;


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
    }
}
