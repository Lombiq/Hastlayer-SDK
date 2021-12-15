using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    public class Posit32FusedCalculator
    {
        public const int CalculateFusedSumInputPosit32CountIndex = 0;
        public const int CalculateFusedSumInputPosit32StartIndex = 1;
        public const int CalculateFusedSumOutputPosit32Index = 0;
        public const int CalculateFusedDotProductInputPosit32CountIndex = 0;
        public const int CalculateFusedDotProductInputPosit32sStartIndex = 1;
        public const int CalculateFusedDotProductOutputPosit32Index = 2;

        public const int MaxArrayChunkSize = 160;
        public const int QuireSizeIn32BitChunks = Posit32.QuireSize >> 5;
        public const int QuireSizeIn64BitChunks = Posit32.QuireSize >> 6;

        public virtual void CalculateFusedSum(SimpleMemory memory)
        {
            var numberCount = memory.ReadUInt32(CalculateFusedSumInputPosit32CountIndex);
            var posit32ArrayChunk = new Posit32[MaxArrayChunkSize];

            var quire = (Quire)new Posit32(0);
            var batchCount = numberCount / MaxArrayChunkSize;

            if (numberCount % MaxArrayChunkSize != 0)
            {
                batchCount += 1;
            }

            for (int i = 0; i < batchCount; i++)
            {
                for (var j = 0; j < posit32ArrayChunk.Length; j++)
                {
                    if (i * MaxArrayChunkSize + j < numberCount)
                    {
                        posit32ArrayChunk[j] = new Posit32(
                            memory.ReadUInt32(CalculateFusedSumInputPosit32StartIndex + i * MaxArrayChunkSize + j),
                            fromBitMask: true);
                    }
                    else posit32ArrayChunk[j] = new Posit32(0);
                }

                quire = Posit32.FusedSum(posit32ArrayChunk, quire);
            }

            var result = new Posit32(quire);
            memory.WriteUInt32(CalculateFusedSumOutputPosit32Index, result.PositBits);
        }
    }

    public static class Posit32FusedCalculatorExtensions
    {
        public static float CalculateFusedSum(
            this Posit32FusedCalculator posit32FusedCalculator,
            uint[] posit32Array,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(posit32Array.Length + 1)
                : hastlayer.CreateMemory(configuration, posit32Array.Length + 1);

            memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSumInputPosit32CountIndex, (uint)posit32Array.Length);

            for (var i = 0; i < posit32Array.Length; i++)
            {
                memory.WriteUInt32(Posit32FusedCalculator.CalculateFusedSumInputPosit32StartIndex + i, posit32Array[i]);
            }

            posit32FusedCalculator.CalculateFusedSum(memory);

            return (float)new Posit32(memory.ReadUInt32(Posit32FusedCalculator.CalculateFusedSumOutputPosit32Index), true);
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
