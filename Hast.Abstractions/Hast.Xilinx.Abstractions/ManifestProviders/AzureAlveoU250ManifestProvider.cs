using Hast.Common.Constants;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.Helpers;

namespace Hast.Xilinx.Abstractions.ManifestProviders;

public class AzureAlveoU250ManifestProvider : IDeviceManifestProvider
{
    public const string DeviceName = "Azure Alveo U250";

    public IDeviceManifest DeviceManifest { get; } =
        new AzureNpDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 300 * Frequency.Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to SimpleMemory
            // the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
            AvailableMemoryBytes = 2 * DataSize.GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[] { "xilinx_u250_gen3x16_xdma_2_1_202010_1" }, // Need a very specific version.
            RequiresDcpBinary = true,
        };

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
