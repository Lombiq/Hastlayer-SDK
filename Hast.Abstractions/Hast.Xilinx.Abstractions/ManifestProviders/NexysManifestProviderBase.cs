using Hast.Communication.Constants.CommunicationConstants;
using Hast.Layer;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.ManifestProviders;

public abstract class NexysManifestProviderBase : IDeviceManifestProvider
{
    protected string _deviceName;

    private IDeviceManifest deviceManifest;

    public IDeviceManifest DeviceManifest =>
        deviceManifest ??= new NexysDeviceManifest
        {
            Name = _deviceName,
            ClockFrequencyHz = 100 * Mhz,
            SupportedCommunicationChannelNames = new[] { Serial.ChannelName, Ethernet.ChannelName },
            AvailableMemoryBytes = 110 * MebiByte,
        };

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        memory.MinimumPrefix = 3;
}
