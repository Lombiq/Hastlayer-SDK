using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class NexysA7Driver : NexysDriverBase
{
    public const string NexysA7 = "Nexys A7";

    public override string DeviceName => NexysA7;

    public NexysA7Driver(ITimingReportParser timingReportParser = null)
        : base(timingReportParser)
    { }
}
