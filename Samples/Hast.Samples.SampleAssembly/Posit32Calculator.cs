using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

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


    public static class Posit32CalculatorExtensions
    {
        public static int CountUpToNumber(this Posit32Calculator positCalculator, int number)
        {
            var memory = new SimpleMemory(1);

            memory.WriteInt32(Posit32Calculator.CalculateLargeIntegerSum_InputInt32Index, number);
            positCalculator.CalculateIntegerSumUpToNumber(memory);

            return memory.ReadInt32(Posit32Calculator.CalculateLargeIntegerSum_OutputInt32Index);
        }
    }
}
