using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AwsF1Driver : VitisDeviceDriverBase
{
    public override string DeviceName => "AWS F1";
    public override string PlatformName => null;
    public override uint ClockFrequencyMhz => 250;

    public AwsF1Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
