using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;


namespace Hast.Samples.SampleAssembly
{
    public class Posit32FusedCalculator
    {
        public const int CalculateFusedSum_InputPosit32CountIndex = 0;
        public const int CalculateFusedSum_InputPosit32StartIndex = 1;
        public const int CalculateFusedSum_OutputPosit32Index = 0;
        public const int CalculateFusedDotProduct_InputPosit32CountIndex = 0;
        public const int CalculateFusedDotProduct_InputPosit32sStartIndex = 1;
        public const int CalculateFusedDotProduct_OutputPosit32Index = 2;    
          

        public virtual void CalculateFusedSum(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(CalculateFusedSum_InputPosit32CountIndex);

            var posit32Array = new Posit32[numberCount];

            for (int i = 0; i < numberCount; i++)
            {
                posit32Array[i] = new Posit32(memory.ReadUInt32(CalculateFusedSum_InputPosit32StartIndex + i), true);
            }
            var result = Posit32.FusedSum(posit32Array);
            memory.WriteUInt32(CalculateFusedSum_OutputPosit32Index, result.PositBits);
        }
    }


    public static class Posit32FusedCalculatorExtensions
    {   

        public static float CalculateFusedSum(this Posit32FusedCalculator posit32FusedCalculator, uint[] posit32Array)
        {
            var memory = new SimpleMemory(posit32Array.Length + 1);

            memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32StartIndex + i, posit32Array[i]);
            }

            posit32FusedCalculator.CalculateFusedSum(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSum_OutputPosit32Index), true);
        }
    }
}
