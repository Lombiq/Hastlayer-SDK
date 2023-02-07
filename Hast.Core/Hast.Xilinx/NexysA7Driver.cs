using Hast.Synthesis.Services;
using Hast.Xilinx.Abstractions.ManifestProviders;

namespace Hast.Xilinx;

public class NexysA7Driver : NexysDriverBase
{
    public NexysA7Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceName = NexysA7ManifestProvider.DeviceName;
}
