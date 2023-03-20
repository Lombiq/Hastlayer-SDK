using Hast.Communication.Constants.CommunicationConstants;
using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class NexysDriverBase : IDeviceDriver
{
    protected readonly string _deviceName;

    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    private IDeviceManifest _deviceManifest;
    private ITimingReport _timingReport;

    public ITimingReport TimingReport
    {
        get
        {
            lock (_timingReportParserLock)
            {
                _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(NexysDriverBase)));

                return _timingReport;
            }
        }
    }

    public IDeviceManifest DeviceManifest =>
        _deviceManifest ??= new NexysDeviceManifest
        {
            Name = _deviceName,
            ClockFrequencyHz = 100 * Mhz,
            SupportedCommunicationChannelNames = new[] { Serial.ChannelName, Ethernet.ChannelName },
            AvailableMemoryBytes = 110 * MebiByte,
        };

    protected NexysDriverBase(string deviceName, ITimingReportParser timingReportParser)
    {
        _deviceName = deviceName;
        _timingReportParser = timingReportParser;
    }

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        memory.MinimumPrefix = 3;
}
