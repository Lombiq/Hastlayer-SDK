using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    /// <summary>
    /// See <see cref="Posit32CalculatorSampleRunner"/> for a more usable example.
    /// </summary>
    internal class PositCalculatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<PositCalculator>();

            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                PositCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                PositCalculatorExtensions.ManuallySizedArrays);
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var positCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new PositCalculator(), configuration);

            var result = positCalculator.CalculateIntegerSumUpToNumber(100000, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
