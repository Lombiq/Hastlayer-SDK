using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class NexysA7Driver : NexysDriverBase
{
    public const string DeviceName = "Nexys A7";

    public NexysA7Driver(ITimingReportParser timingReportParser)
        : base(DeviceName, timingReportParser)
    { }
}
