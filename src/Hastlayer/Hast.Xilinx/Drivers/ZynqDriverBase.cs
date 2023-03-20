using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.Helpers;
using System;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public abstract class ZynqDriverBase : IDeviceDriver
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
                _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(ZynqDriverBase)));

                return _timingReport;
            }
        }
    }

    public IDeviceManifest DeviceManifest =>
        _deviceManifest ??= new ZynqDeviceManifest
        {
            Name = _deviceName,
            ClockFrequencyHz = 150 * Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            AvailableMemoryBytes = 1 * GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[]
            {
                _deviceName.RegexReplace(@"[^A-Za-z0-9]+", "-"),
                "hw_platform",
            },
            // The frequency is set by ZynqHardwareImplementationComposerBuildProvider after build.
            BuildWithClockFrequencyHz = false,
            AxiBusWith = 1024,
        };

    protected ZynqDriverBase(string deviceName, ITimingReportParser timingReportParser)
    {
        _deviceName = deviceName;
        _timingReportParser = timingReportParser;
    }

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
