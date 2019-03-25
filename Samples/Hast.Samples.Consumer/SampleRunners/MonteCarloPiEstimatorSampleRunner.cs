using System;
using System.Threading.Tasks;
using Hast.Algorithms;
using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class MonteCarloPiEstimatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<MonteCarloPiEstimator>();
            configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomXorshiftLfsr16>(p => p.NextUInt16());
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            uint iterationsCount = MonteCarloPiEstimator.MaxDegreeOfParallelism * 5000000;

            // On a Nexys 4 DDR this takes about 340ms with a 76 degree of parallelism and method inlining. It takes
            // about 1,5s on an i7 processor with 4 physical (8 logical) cores.

            var monteCarloPiEstimator = await hastlayer.GenerateProxy(hardwareRepresentation, new MonteCarloPiEstimator());
            var piEstimateHardware = monteCarloPiEstimator.EstimatePi(iterationsCount);
            Console.WriteLine("Estimate for Pi on hardware: " + piEstimateHardware);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var piEstimateSoftware = new MonteCarloPiEstimator().EstimatePi(iterationsCount);
            sw.Stop();
            Console.WriteLine("Estimate for Pi on software: " + piEstimateSoftware);
            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
