using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AlveoU200Driver : VitisDeviceDriverBase
{
    public override string DeviceName => "Alveo U200";
    public override string PlatformName => "xilinx_u200";
    public override uint ClockFrequencyMhz => 300;

    public AlveoU200Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
