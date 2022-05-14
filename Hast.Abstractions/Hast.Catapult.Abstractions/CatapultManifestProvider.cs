using Hast.Layer;
using Hast.Synthesis.Abstractions;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Catapult.Abstractions;

public class CatapultManifestProvider : IDeviceManifestProvider
{
    public const string DeviceName = "Catapult";

    public IDeviceManifest DeviceManifest { get; } =
        new CatapultDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 150 * Mhz,
            // Since it's completely Catapult-specific, not using e.g. "PCIe" here.
            SupportedCommunicationChannelNames = new[] { DeviceName },
            // Right now the whole memory is not available due to one physical cell being equal to one logical one.
            AvailableMemoryBytes = 8 * GigaByte / 16,
        };

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration)
    {
        // Method intentionally left empty. There is nothing to do with this device.
    }
}
