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

        public const int MaxInputArraySize = 100000;

        public virtual void CalculateFusedSum(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(CalculateFusedSum_InputPosit32CountIndex);

            var posit32Array = new Posit32[MaxInputArraySize];

            for (var i = 0; i < numberCount; i++)
            {
                posit32Array[i] = new Posit32(memory.ReadUInt32(CalculateFusedSum_InputPosit32StartIndex + i), true);
            }
            for (var i = numberCount; i < MaxInputArraySize; i++) posit32Array[i] = new Posit32(0);
            var result = Posit32.FusedSum(posit32Array);
            memory.WriteUInt32(CalculateFusedSum_OutputPosit32Index, result.PositBits);
        }
    }


    public static class Posit32FusedCalculatorExtensions
    {

        public static float CalculateFusedSum(this Posit32FusedCalculator posit32FusedCalculator, uint[] posit32Array)
        {
            if (posit32Array.Length > Posit32FusedCalculator.MaxInputArraySize) throw new IndexOutOfRangeException("The maximum number of posits to be summed with the fused sum operation can not exceed the MaxInPutArraySize specified in the Posit32FusedCalculator class.");
            var memory = new SimpleMemory(posit32Array.Length + 1);

            memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32StartIndex + i, posit32Array[i]);
            }

            posit32FusedCalculator.CalculateFusedSum(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSum_OutputPosit32Index), true);
        }

        public static readonly string[] ManuallySizedArrays = new[]
        {
           "System.UInt64[] Lombiq.Arithmetics.Quire::Segments()",
           "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_Addition(Lombiq.Arithmetics.Quire,Lombiq.Arithmetics.Quire).array",
           "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_RightShift(Lombiq.Arithmetics.Quire,System.Int32).array",
           "Lombiq.Arithmetics.Quire Lombiq.Arithmetics.Quire::op_LeftShift(Lombiq.Arithmetics.Quire,System.Int32).array"
        };
    }
}
