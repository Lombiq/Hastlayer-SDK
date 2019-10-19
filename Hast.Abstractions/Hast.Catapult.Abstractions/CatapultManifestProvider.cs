using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Catapult.Abstractions
{
    public class CatapultManifestProvider : IDeviceManifestProvider
    {
        public const string DeviceName = "Catapult";

        public IDeviceManifest DeviceManifest { get; } =
            new DeviceManifest
            {
                Name = DeviceName,
                ClockFrequencyHz = 150000000, // 150 Mhz
                // Since it's completely Catapult-specific, not using e.g. "PCIe" here.
                SupportedCommunicationChannelNames = new[] { DeviceName },
                // Right now the whole memory is not available due to one physical cell being equal to one logical one.
                AvailableMemoryBytes = 8_000_000_000UL / 16,
                DataBusWidthBytes = 64
            };
    }
}
