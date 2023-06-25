using Hast.Synthesis.Services;

namespace Hast.Xilinx.Drivers;

public class AzureAlveoU250Driver : VitisDeviceDriverBase
{
    public const string AzureAlveoU250 = "Azure Alveo U250";

    public override string DeviceName => AzureAlveoU250;
    public override string PlatformName => "xilinx_u250_gen3x16_xdma_2_1_202010_1"; // Needs a very specific version.
    public override uint ClockFrequencyMhz => 300;

    public AzureAlveoU250Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser)
    { }
}
