using Hast.Synthesis.Services;
using Hast.Xilinx.ManifestProviders;

namespace Hast.Xilinx;

public class ZedBoardDriver : ZynqDriverBase
{
    public ZedBoardDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceName = ZedBoardManifestProvider.DeviceName;
}
