using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class Posit32FusedCalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit32FusedCalculator>();

            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
               Posit32.QuireSize >> 6,
               Posit32FusedCalculatorExtensions.ManuallySizedArrays);
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            RunSoftwareBenchmarks();

            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32FusedCalculator());

        }

        public static void RunSoftwareBenchmarks()
        {
            var positCalculator = new Posit32FusedCalculator();

            var posit32ArrayChunk = new uint[Posit32FusedCalculator.MaxInputArraySize];
            // All positive integers smaller than this value ("pintmax") can be exactly represented with 32-bit Posits.
            var quireStartingValue = (Quire)new Posit32(8388608);

            for (var i = 0; i < Posit32FusedCalculator.MaxInputArraySize; i++)
            {
                posit32ArrayChunk[i] = new Posit32(1).PositBits;
            }

            // Not to run the benchmark below the first time, because JIT compiling can affect it.           
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 500; i++)
            {
                quireStartingValue = positCalculator.CalculateFusedSum(posit32ArrayChunk, quireStartingValue);
            }
            var positsInArrayFusedSum = new Posit32(quireStartingValue);
            sw.Stop();

            Console.WriteLine("Result of Fused addition of posits in array: " + (float)positsInArrayFusedSum);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();
        }
    }
}
