﻿using System.Linq;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.Helpers;
using Newtonsoft.Json.Linq;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public class AlveoU200ManifestProvider : IDeviceManifestProvider
    {
        public const string DeviceName = "Alveo U200";

        public IDeviceManifest DeviceManifest { get; } =
            new XilinxDeviceManifest
            {
                Name = DeviceName,
                ClockFrequencyHz = 300000000, // 300 Mhz
                SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
                // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to
                // SimpleMemory the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
                AvailableMemoryBytes = 2_000_000_000UL,
                ToolChainName = CommonToolChainNames.Vivado,
                SupportsHbm = false
            };

        public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
            MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
    }
}
