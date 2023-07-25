using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class TrenzTE071504301CDriver : ZynqDriverBase
{
    public const string TrenzTE071504301C = "TE0715-04-30-1C";

    public override string DeviceName => TrenzTE071504301C;

    public TrenzTE071504301CDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
