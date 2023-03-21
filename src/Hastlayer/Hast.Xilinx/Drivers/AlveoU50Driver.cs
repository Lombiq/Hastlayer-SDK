using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AlveoU50Driver : VitisDeviceDriverBase
{
    public override string DeviceName => "Alveo U50";
    public override string PlatformName => "xilinx_u50_gen3x16";
    public override uint ClockFrequencyMhz => 300;

    public AlveoU50Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
