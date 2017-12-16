using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class PositCalculator
    {
        public const int CalculateLargeIntegerSum_InputInt32Index = 0;
        public const int CalculateLargeIntegerSum_OutputInt32Index = 0;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSum_InputInt32Index);

            var a = new Posit32(1);
            var b = a;

            for (var i = 1; i < number; i++)
            {
                a += b;
            }
            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSum_OutputInt32Index, result);
        }
    }


    public static class PositCalculatorExtensions
    {
        public static int CountUpToNumber(this PositCalculator positCalculator, int number)
        {
            var memory = new SimpleMemory(1);

            memory.WriteInt32(PositCalculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(PositCalculator.CalculateLargeIntegerSum_OutputInt32Index);
        }
    }
}
