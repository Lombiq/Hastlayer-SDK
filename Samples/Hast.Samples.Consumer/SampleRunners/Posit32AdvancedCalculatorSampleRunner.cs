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

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            RunSoftwareBenchmarks();
            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32AdvancedCalculator(), configuration ?? ProxyGenerationConfiguration.Default);

            positCalculator.RepeatedDivision(10, (float)153157.898526, (float)3.3);

            var sqrtInputArray = new uint[10];
            for (int i = 0; i < 10; i++)
            {
                sqrtInputArray[i] = new Posit32((float)(i + 1) * (i + 1)).PositBits;
            }

            positCalculator.SqrtOfPositsInArray(sqrtInputArray);
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

            var sqrtInputArray = new uint[10];
            for (int i = 0; i < 10; i++)
            {
                sqrtInputArray[i] = new Posit32((float)(i + 1) * (i + 1)).PositBits;
            }
            sw = Stopwatch.StartNew();
            var resultOfSqrt = positCalculator.SqrtOfPositsInArray(sqrtInputArray);
            sw.Stop();

            Console.WriteLine("Result of sqrt: ");
            for (int i = 0; i < resultOfSqrt.Length; i++)
            {
                Console.Write(resultOfSqrt[i] + ", ");
            }
            Console.WriteLine();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds + "ms");
        }
    }
}
