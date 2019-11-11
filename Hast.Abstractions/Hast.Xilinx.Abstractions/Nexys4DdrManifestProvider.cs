using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Xilinx.Abstractions
{
    public class Nexys4DdrManifestProvider : IDeviceManifestProvider
    {
        public const string DeviceName = "Nexys4 DDR";

        public IDeviceManifest DeviceManifest { get; } =
            new DeviceManifest
            {
                Name = DeviceName,
                ClockFrequencyHz = 100000000, // 100 Mhz
                SupportedCommunicationChannelNames = new[] { "Serial", "Ethernet" },
                AvailableMemoryBytes = 115343360 // 110MB
            };
    }
}
