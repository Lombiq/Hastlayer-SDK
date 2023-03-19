using Hast.Catapult.Models;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx;

namespace Hast.Catapult;

public class CatapultDriver : CatapultManifestProvider, IDeviceDriver
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
                _timingReport ??= _timingReportParser.Parse(
                    ResourceHelper.GetTimingReport(
                        nameof(CatapultDriver),
                        typeof(CatapultDriver).Assembly));

                return _timingReport;
            }
        }
    }

    public CatapultDriver(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;
}
