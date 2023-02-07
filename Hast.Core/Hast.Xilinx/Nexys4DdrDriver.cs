using Hast.Synthesis.Services;
using Hast.Xilinx.Abstractions.ManifestProviders;

namespace Hast.Xilinx;

public class Nexys4DdrDriver : NexysDriverBase
{
    public Nexys4DdrDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceName = Nexys4DdrManifestProvider.DeviceName;
}
