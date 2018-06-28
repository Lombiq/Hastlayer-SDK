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
    internal class Posit32AdvancedCalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit32AdvancedCalculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            RunSoftwareBenchmarks();
            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32AdvancedCalculator());

            positCalculator.RepeatedDivision(10, (float)153157.898526, (float)3.3);
        }

        public static void RunSoftwareBenchmarks()
        {
            var positCalculator = new Posit32AdvancedCalculator();


            positCalculator.RepeatedDivision(10, (float)153157.898526, (float)3.3);
            var sw = Stopwatch.StartNew();
            var resultOfDivision = positCalculator.RepeatedDivision(10, (float)153157.898526, (float)3.3);
            sw.Stop();

            Console.WriteLine("Result of repeated division: " + resultOfDivision);
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");
        }

    }
}
