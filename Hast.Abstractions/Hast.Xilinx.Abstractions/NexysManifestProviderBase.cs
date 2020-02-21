using Hast.Layer;
using Hast.Synthesis.Abstractions;
using System.Collections.Generic;

namespace Hast.Xilinx.Abstractions
{
    public abstract class NexysManifestProviderBase : IDeviceManifestProvider
    {
        protected static Dictionary<string, string> DeviceNameInternals = new Dictionary<string, string>();

        private IDeviceManifest deviceManifest = null;
        public IDeviceManifest DeviceManifest
        {
            get
            {
                if (deviceManifest is null)
                {
                    deviceManifest = new DeviceManifest
                    {
                        Name = DeviceNameInternals[GetType().Name],
                        ClockFrequencyHz = 100000000, // 100 Mhz
                        SupportedCommunicationChannelNames = new[] { "Serial", "Ethernet" },
                        AvailableMemoryBytes = 115343360 // 110MB
                    };
                }
                return deviceManifest;
            }
        }
            
    }
}
