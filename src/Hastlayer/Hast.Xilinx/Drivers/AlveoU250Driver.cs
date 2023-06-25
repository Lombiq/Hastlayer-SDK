using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AlveoU250Driver : VitisDeviceDriverBase
{
    public const string AlveoU250 = "Alveo U250";

    public override string DeviceName => AlveoU250;
    public override string PlatformName => "xilinx_u250";
    public override uint ClockFrequencyMhz => 300;

    public AlveoU250Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
