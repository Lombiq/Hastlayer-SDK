using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.ManifestProviders;

namespace Hast.Xilinx;

public class AlveoU280Driver : AlveoU280ManifestProvider, IDeviceDriver
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
                _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(AlveoU280Driver)));

                return _timingReport;
            }
        }
    }

    public AlveoU280Driver(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;
}
