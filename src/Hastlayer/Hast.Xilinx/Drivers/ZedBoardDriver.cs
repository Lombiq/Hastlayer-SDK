using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class ZedBoardDriver : ZynqDriverBase
{
    public override string DeviceName => "ZedBoard";

    public ZedBoardDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
