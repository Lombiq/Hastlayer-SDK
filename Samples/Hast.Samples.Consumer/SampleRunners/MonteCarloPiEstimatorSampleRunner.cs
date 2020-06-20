using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class MonteCarloPiEstimatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<MonteCarloPiEstimator>();
            configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomXorshiftLfsr16>(p => p.NextUInt16());
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            uint iterationsCount = MonteCarloPiEstimator.MaxDegreeOfParallelism * 500000;

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var monteCarloPiEstimator = await hastlayer.GenerateProxy(hardwareRepresentation, new MonteCarloPiEstimator(), configuration);
            var piEstimateHardware = monteCarloPiEstimator.EstimatePi(iterationsCount, memoryConfig);
            Console.WriteLine("Estimate for Pi on hardware: " + piEstimateHardware);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var piEstimateSoftware = new MonteCarloPiEstimator().EstimatePi(iterationsCount, memoryConfig);
            sw.Stop();
            Console.WriteLine("Estimate for Pi on software: " + piEstimateSoftware);
            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
