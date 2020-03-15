using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public abstract class NexysManifestProviderBase : IDeviceManifestProvider
    {
        protected static string DeviceNameInternal;

        public IDeviceManifest DeviceManifest { get; } =
            new DeviceManifest
            {
                Name = DeviceNameInternal,
                ClockFrequencyHz = 100000000, // 100 Mhz
                SupportedCommunicationChannelNames = new[] { "Serial", "Ethernet" },
                AvailableMemoryBytes = 115343360, // 110MB
                ToolChainName = CommonToolChainNames.Vivado
            };
    }
}
