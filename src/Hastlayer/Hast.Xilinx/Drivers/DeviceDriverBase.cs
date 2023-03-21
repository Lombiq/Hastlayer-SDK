using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using System;

namespace Hast.Xilinx.Drivers;

public abstract class DeviceDriverBase : IDeviceDriver
{
    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    protected Lazy<IDeviceManifest> _deviceManifest;
    private ITimingReport _timingReport;

    protected virtual string TimingReportFileName => null;

    public abstract string DeviceName { get; }

    public ITimingReport TimingReport
    {
        get
        {
            lock (_timingReportParserLock)
            {
                _timingReport ??= _timingReportParser.Parse(
                    ResourceHelper.GetTimingReport(
                        TimingReportFileName ?? GetType().Name));

                return _timingReport;
            }
        }
    }

    public IDeviceManifest DeviceManifest => _deviceManifest.Value;

    protected DeviceDriverBase(ITimingReportParser timingReportParser) =>
        _timingReportParser = timingReportParser;

    public abstract void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);
}
