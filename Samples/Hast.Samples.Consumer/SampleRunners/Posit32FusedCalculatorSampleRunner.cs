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

            var posit32Array = new uint[100000];
            // All positive integers smaller than this value ("pintmax") can be exactly represented with 32-bit Posits.
            posit32Array[0] = new Posit32(8388608).PositBits;

            for (var i = 1; i < posit32Array.Length; i++)
            {
                posit32Array[i] = new Posit32(1).PositBits;
            }

            // Not to run the benchmark below the first time, because JIT compiling can affect it.           
            var sw = Stopwatch.StartNew();
            var result = positCalculator.CalculateFusedSum(posit32Array);

            sw.Stop();

            Console.WriteLine("Result of Fused addition of posits in array: " + result);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");

            Console.WriteLine();
        }
    }
}
