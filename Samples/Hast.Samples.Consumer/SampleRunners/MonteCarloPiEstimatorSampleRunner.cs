using System;
using System.Threading.Tasks;
using Hast.Algorithms;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class MonteCarloPiEstimatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<MonteCarloPiEstimator>();
            configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomLfsr>(p => p.NextUInt32());
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            uint iterationsCount = MonteCarloPiEstimator.MaxDegreeOfParallelism * 5000000;

            // On a Nexys A7 this takes about 1,7s with a 36 degree of parallelism of and method inlining. Although all
            // the DSPs are utilized on the FPGA since there are plenty of the other resources it's possible to use
            // more parallel executors. It takes about 1,1s on an i7 processor with 4 physical (8 logical) cores.

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
