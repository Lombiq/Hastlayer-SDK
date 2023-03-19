using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.ManifestProviders;

namespace Hast.Xilinx.Drivers;

public abstract class ZynqDriverBase : ZynqManifestProviderBase, IDeviceDriver
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
                _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(ZynqDriverBase)));

                return _timingReport;
            }
        }
    }

    protected ZynqDriverBase(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;
}
