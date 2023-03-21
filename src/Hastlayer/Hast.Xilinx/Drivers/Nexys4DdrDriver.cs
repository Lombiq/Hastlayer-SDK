using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class Nexys4DdrDriver : NexysDriverBase
{
    public override string DeviceName => "Nexys4 DDR";

    public Nexys4DdrDriver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
