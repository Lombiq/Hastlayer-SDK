using System.Linq;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Newtonsoft.Json.Linq;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public class AlveoU200ManifestProvider : IDeviceManifestProvider
    {
        public const string DeviceName = "Alveo U200";

        public IDeviceManifest DeviceManifest { get; } =
            new DeviceManifest
            {
                Name = DeviceName,
                ClockFrequencyHz = 300000000, // 300 Mhz
                SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
                // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to
                // SimpleMemory the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
                AvailableMemoryBytes = 2_000_000_000UL,
                ToolChainName = CommonToolChainNames.Vivado
            };

        public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
            ConfigureMemoryForVitis(memory, hardwareGeneration);

        public static void ConfigureMemoryForVitis(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration)
        {
            memory.Alignment = 4096;
            memory.MinimumPrefix = 4;

            if (!hardwareGeneration.CustomConfiguration.TryGetValue("OpenClConfiguration", out var value)) return;
            var custom = (value is JObject jObject ? jObject : JObject.FromObject(value))
                .Properties()
                .ToDictionary(x => x.Name, x => x.Value);
            if (custom.TryGetValue(nameof(memory.Alignment), out var alignment)) memory.Alignment = alignment.Value<int>();
            if (custom.TryGetValue(nameof(memory.MinimumPrefix), out var prefix)) memory.MinimumPrefix = prefix.Value<int>();
        }
    }
}
