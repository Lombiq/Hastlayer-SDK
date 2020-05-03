using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public abstract class NexysManifestProviderBase : IDeviceManifestProvider
    {
        protected string _deviceName;

        private IDeviceManifest deviceManifest = null;
        public IDeviceManifest DeviceManifest
        {
            get
            {
                if (deviceManifest is null)
                {
                    deviceManifest = new DeviceManifest
                    {
                        Name = _deviceName,
                        ClockFrequencyHz = 100000000, // 100 Mhz
                        SupportedCommunicationChannelNames = new[] { "Serial", "Ethernet" },
                        AvailableMemoryBytes = 115343360, // 110MB
                        ToolChainName = CommonToolChainNames.Vivado
                    };
                }
                return deviceManifest;
            }
        }

    }
}
