using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.ManifestProviders;

namespace Hast.Xilinx;

public class AzureAlveoU250Driver : AzureAlveoU250ManifestProvider, IDeviceDriver
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
                return _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(AlveoU250Driver)));
            }
        }
    }

    public AzureAlveoU250Driver(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;
}
