using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Configuration;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal sealed class UnumCalculatorSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration)
    {
        configuration.AddHardwareEntryPointType<UnumCalculator>();

        configuration.TransformerConfiguration().AddLengthForMultipleArrays(
            UnumCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
            UnumCalculatorExtensions.ManuallySizedArrays);
    }

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        var unumCalculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new UnumCalculator(), configuration);
        _ = unumCalculator.CalculateSumOfPowersofTwo(9, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
    }
}
