using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal class LoopbackSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<Loopback>();

    public async Task RunAsync(
        IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration)
    {
        var loopback = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Loopback(), configuration);

        _ = loopback.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = loopback.Run(1234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = loopback.Run(-9, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = loopback.Run(0, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = loopback.Run(-19, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = loopback.Run(1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
    }
}
