using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class TrenzTE071504301CDriver : ZynqDriverBase
{
    public override string DeviceName => "TE0715-04-30-1C";

    public TrenzTE071504301CDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
