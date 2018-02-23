using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                AvailableMemoryBytes = 4 * 1024 * 1024
            };
    }
}
