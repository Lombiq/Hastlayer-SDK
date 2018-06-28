using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class Posit32AdvancedCalculator
    {

        public const int RepeatedDivision_InputInt32Index = 0;
        public const int RepeatedDivision_FirstInputPosit32Index = 1;
        public const int RepeatedDivision_SecondInputPosit32Index = 2;
        public const int RepeatedDivision_OutputPosit32Index = 0;
        public virtual void RepeatedDivision(SimpleMemory memory)
        {
            var number = memory.ReadInt32(RepeatedDivision_InputInt32Index);
            var dividendPosit = memory.ReadUInt32(RepeatedDivision_FirstInputPosit32Index);
            var divisorPosit = memory.ReadUInt32(RepeatedDivision_SecondInputPosit32Index);

            var a = new Posit32(dividendPosit, true);
            var b = new Posit32(divisorPosit, true);

            for (uint i = 0; i < number; i++)
            {
                a /= b;
            }

            var result = a.PositBits;
            memory.WriteUInt32(RepeatedDivision_OutputPosit32Index, result);
        }
    }

    public static class Posit32AdvancedCalculatorExtensions
    {       
        public static float RepeatedDivision(this Posit32AdvancedCalculator positCalculator, int number, float dividend, float divisor)
        {
            var memory = new SimpleMemory(3);

            memory.WriteInt32(Posit32AdvancedCalculator.RepeatedDivision_InputInt32Index, number);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivision_FirstInputPosit32Index, new Posit32(dividend).PositBits);
            memory.WriteUInt32(Posit32AdvancedCalculator.RepeatedDivision_SecondInputPosit32Index, new Posit32(divisor).PositBits);

            positCalculator.RepeatedDivision(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32AdvancedCalculator.RepeatedDivision_OutputPosit32Index), true);
        }      
    }
}
