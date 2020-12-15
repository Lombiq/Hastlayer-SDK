using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class UnumCalculatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<UnumCalculator>();

            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                UnumCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                UnumCalculatorExtensions.ManuallySizedArrays);
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var unumCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new UnumCalculator(), configuration);

            var result = unumCalculator.CalculateSumOfPowersofTwo(9, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
