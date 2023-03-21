using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public abstract class DeviceDriverBase : IDeviceDriver
{
    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    private ITimingReport _timingReport;

    public ITimingReport TimingReport
    {
        get
        {
            lock (_timingReportParserLock)
            {
                _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(GetType().Name));

                return _timingReport;
            }
        }
    }

    public abstract IDeviceManifest DeviceManifest { get; }

    protected DeviceDriverBase(ITimingReportParser timingReportParser) =>
        _timingReportParser = timingReportParser;

    public abstract void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);
}
