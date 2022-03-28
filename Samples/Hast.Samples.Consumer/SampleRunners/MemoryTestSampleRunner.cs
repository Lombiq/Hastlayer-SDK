using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal class MemoryTestSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<MemoryTest>();

    public async Task RunAsync(
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration)
    {
        var memoryTest = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new MemoryTest(), configuration);

        _ = memoryTest.Run(0, 1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(0, 3, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(0, 7, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(0, 50, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(1, 1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(3, 7, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = memoryTest.Run(47, 100, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
    }
}
