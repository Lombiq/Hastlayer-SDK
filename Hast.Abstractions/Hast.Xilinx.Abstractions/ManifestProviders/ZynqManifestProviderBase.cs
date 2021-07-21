using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.Helpers;
using System.Text.RegularExpressions;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public abstract class ZynqManifestProviderBase : IDeviceManifestProvider
    {
        protected string _deviceName;

        private IDeviceManifest _deviceManifest;
        public IDeviceManifest DeviceManifest =>
            _deviceManifest ??= new XilinxDeviceManifest
            {
                Name = _deviceName,
                ClockFrequencyHz = 150 * Mhz,
                SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
                AvailableMemoryBytes = 1 * GigaByte,
                SupportsHbm = false,
                SupportedPlatforms = new[]
                {
                    Regex.Replace(_deviceName.ToLower(), @"[^a-z0-9]+", "-"),
                    "hw_platform",
                },
                ToolChainName = CommonToolChainNames.Vitis,
            };

        public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
            MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
    }
}
