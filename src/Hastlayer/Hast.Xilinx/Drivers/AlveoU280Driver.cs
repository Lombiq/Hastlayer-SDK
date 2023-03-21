using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AlveoU280Driver : VitisDeviceDriverBase
{
    public override string DeviceName => "Alveo U280";
    public override string PlatformName => "xilinx_u280";
    public override uint ClockFrequencyMhz => 300;

    public AlveoU280Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
