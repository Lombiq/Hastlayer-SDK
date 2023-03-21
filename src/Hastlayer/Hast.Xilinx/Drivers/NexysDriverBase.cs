using Hast.Communication.Constants.CommunicationConstants;
using Hast.Layer;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class NexysDriverBase : DeviceDriverBase
{
    protected override string TimingReportFileName => nameof(NexysDriverBase);

    protected NexysDriverBase(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceManifest = new Lazy<IDeviceManifest>(() => new NexysDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 100 * Mhz,
            SupportedCommunicationChannelNames = new[] { Serial.ChannelName, Ethernet.ChannelName },
            AvailableMemoryBytes = 110 * MebiByte,
        });

    public override void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        memory.MinimumPrefix = 3;
}
