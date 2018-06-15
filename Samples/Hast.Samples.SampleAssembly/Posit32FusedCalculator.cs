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
        public const int CalculateFusedSum_InputQuireStartIndex = 0;
        public const int CalculateFusedSum_InputPosit32CountIndex = QuireSizeIn32BitChunks;
        public const int CalculateFusedSum_InputPosit32StartIndex = QuireSizeIn32BitChunks + 1;
        public const int CalculateFusedSum_OutputQuireIndex = 0;
        //public const int CalculateFusedSum_OutputPosit32Index = QuireSizeIn32BitChunks;
        public const int CalculateFusedDotProduct_InputPosit32CountIndex = 0;
        public const int CalculateFusedDotProduct_InputPosit32sStartIndex = 1;
        public const int CalculateFusedDotProduct_OutputPosit32Index = 2;

        public const int MaxInputArraySize = 200;
        public const int QuireSizeIn32BitChunks = Posit32.QuireSize >> 5;
        public const int QuireSizeIn64BitChunks = Posit32.QuireSize >> 6;



        public virtual void CalculateFusedSum(SimpleMemory memory)
        {
            uint numberCount = memory.ReadUInt32(CalculateFusedSum_InputPosit32CountIndex);
            var quireSegments = new ulong[QuireSizeIn64BitChunks];


            for (var i = 0; i < QuireSizeIn64BitChunks; i++)
            {
                quireSegments[i] = memory.ReadUInt32(CalculateFusedSum_InputQuireStartIndex + i * 2);
                quireSegments[i] <<= 32;
                quireSegments[i] += memory.ReadUInt32(CalculateFusedSum_InputQuireStartIndex + i * 2 + 1);
            }

            var quireStartValue = new Quire(quireSegments);
            var posit32Array = new Posit32[MaxInputArraySize];

            for (var i = 0; i < numberCount; i++)
            {
                posit32Array[i] = new Posit32(memory.ReadUInt32(CalculateFusedSum_InputPosit32StartIndex + i), true);
            }

            for (var i = numberCount; i < MaxInputArraySize; i++) posit32Array[i] = new Posit32(0);

            var resultQuire = Posit32.FusedSum(posit32Array, quireStartValue);
            for (var i = 0; i < QuireSizeIn64BitChunks; i++)
            {
                memory.WriteUInt32(CalculateFusedSum_OutputQuireIndex + i * 2, (uint)(resultQuire.Segments[i] >> 32));
                memory.WriteUInt32(CalculateFusedSum_OutputQuireIndex + i * 2 + 1, (uint)resultQuire.Segments[i]);
            }

        }
    }


    public static class Posit32FusedCalculatorExtensions
    {

        public static Quire CalculateFusedSum(this Posit32FusedCalculator posit32FusedCalculator, uint[] posit32Array, Quire quireStartingValue)
        {
            if (posit32Array.Length > Posit32FusedCalculator.MaxInputArraySize)
            {
                throw new IndexOutOfRangeException("The maximum number of posits to be summed with the fused sum operation can not exceed the MaxInPutArraySize specified in the Posit32FusedCalculator class.");
            }

            var memory = new SimpleMemory(posit32Array.Length + Posit32FusedCalculator.QuireSizeIn32BitChunks + 1);

            for (var i = 0; i < quireStartingValue.SegmentCount; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputQuireStartIndex + (i * 2), (uint)(quireStartingValue.Segments[i] >> 32));
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputQuireStartIndex + (i * 2) + 1, (uint)quireStartingValue.Segments[i]);
            }

            memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSum_InputPosit32StartIndex + i, posit32Array[i]);
            }

            posit32FusedCalculator.CalculateFusedSum(memory);

            var resultQuireSegments = new ulong[Posit32FusedCalculator.QuireSizeIn64BitChunks];

            for (var i = 0; i < Posit32FusedCalculator.QuireSizeIn64BitChunks; i++)
            {
                resultQuireSegments[i] = memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSum_OutputQuireIndex + i * 2);
                resultQuireSegments[i] <<= 32;
                resultQuireSegments[i] += memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSum_OutputQuireIndex + i * 2 + 1);
            }

            var resultQuire = new Quire(resultQuireSegments);

            return resultQuire;
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
