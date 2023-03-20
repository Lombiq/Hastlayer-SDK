using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class ZedBoardDriver : ZynqDriverBase
{
    public const string DeviceName = "ZedBoard";

    public ZedBoardDriver(ITimingReportParser timingReportParser)
        : base(DeviceName, timingReportParser)
    { }
}
