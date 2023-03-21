using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class Nexys4DdrDriver : NexysDriverBase
{
    public const string Nexys4Ddr = "Nexys4 DDR";

    public override string DeviceName => Nexys4Ddr;

    public Nexys4DdrDriver(ITimingReportParser timingReportParser = null)
        : base(timingReportParser)
    { }
}
