using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.HelpfulLibraries.Libraries.Utilities;
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

        public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var maxDegreeOfParallelism = (uint)MonteCarloPiEstimator.MaxDegreeOfParallelism;
            uint iterationsCount = maxDegreeOfParallelism * 500000;

            var monteCarloPiEstimator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new MonteCarloPiEstimator(), configuration);
            var piEstimateHardware = monteCarloPiEstimator.EstimatePi(
                iterationsCount,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
            Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Estimate for Pi on hardware: ", piEstimateHardware));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var piEstimateSoftware = new MonteCarloPiEstimator().EstimatePi(
                iterationsCount,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();
            Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("Estimate for Pi on software: ", piEstimateSoftware));
            Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("On CPU it took ", sw.ElapsedMilliseconds, "ms."));
        }
    }
}
