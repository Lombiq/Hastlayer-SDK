using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Communication.Constants.CommunicationConstants;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public abstract class NexysManifestProviderBase : IDeviceManifestProvider
    {
        protected string _deviceName;

        private IDeviceManifest deviceManifest;
        public IDeviceManifest DeviceManifest =>
            deviceManifest ??= new XilinxDeviceManifest
            {
                Name = _deviceName,
                ClockFrequencyHz = 100000000, // 100 Mhz
                SupportedCommunicationChannelNames = new[] { Serial.ChannelName, Ethernet.ChannelName },
                AvailableMemoryBytes = 115343360, // 110MB
                SupportsHbm = false,
                ToolChainName = CommonToolChainNames.Vivado,
            };

        public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
            memory.MinimumPrefix = 3;
    }
}
