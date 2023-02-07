using Hast.Synthesis.Services;
using Hast.Xilinx.Abstractions.ManifestProviders;

namespace Hast.Xilinx;

public class TrenzTE071504301CDriver : ZynqDriverBase
{
    public TrenzTE071504301CDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceName = TrenzTE071504301CManifestProvider.DeviceName;
}
