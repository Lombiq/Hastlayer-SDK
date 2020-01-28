using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System;
using System.Threading.Tasks;

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
            uint iterationsCount = MonteCarloPiEstimator.MaxDegreeOfParallelism * 500000;

            // On a Nexys A7 this takes about 34 ms with a 78 degree of parallelism and method inlining (61% of the
            // FPGA resources utilized). It takes about 120 ms on an i7 processor with 4 physical (8 logical) cores.
            // On Catapult with a 350 degree of parallelism it takes 26 ms on hardware and 160 ms on the (2*32 logical 
            // core) CPU.

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
