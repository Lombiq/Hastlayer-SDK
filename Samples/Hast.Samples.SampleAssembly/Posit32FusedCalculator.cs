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
                        posit32ArrayChunk[j] = new Posit32(memory.ReadUInt32(CalculateFusedSumInputPosit32StartIndex + i * MaxArrayChunkSize + j), true);
                    }
                    else posit32ArrayChunk[j] = new Posit32(0);
                }

                quire = Posit32.FusedSum(posit32ArrayChunk, quire);
            }

            var result = new Posit32(quire);
            memory.WriteUInt32(CalculateFusedSumOutputPosit32Index, result.PositBits);
        }
    }
}
