using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.Helpers;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Abstractions.ManifestProviders;

public class AlveoU250ManifestProvider : IDeviceManifestProvider
{
    public const string DeviceName = "Alveo U250";

    public IDeviceManifest DeviceManifest { get; } =
        new VitisDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 300 * Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to SimpleMemory
            // the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
            AvailableMemoryBytes = 2 * GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[] { "xilinx_u250" },
        };

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
