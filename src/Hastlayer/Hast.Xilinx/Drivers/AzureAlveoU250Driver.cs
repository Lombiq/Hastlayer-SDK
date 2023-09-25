using Hast.Layer;
using Hast.Synthesis.Services;
using System;

namespace Hast.Xilinx.Drivers;

public class AzureAlveoU250Driver : VitisDeviceDriverBase
{
    public const string AzureAlveoU250 = "Azure Alveo U250";

    public override string DeviceName => AzureAlveoU250;
    public override string PlatformName => "xilinx_u250_gen3x16_xdma_2_1_202010_1"; // Needs a very specific version.
    public override uint ClockFrequencyMhz => 300;

    protected override string TimingReportFileName => nameof(AlveoU250Driver);

    public AzureAlveoU250Driver(ITimingReportParser timingReportParser)
        : base(timingReportParser) =>
        _deviceManifest = new Lazy<IDeviceManifest>(() => InitializeManifest(new AzureNpDeviceManifest
        {
            SupportsHbm = false,
            RequiresDcpBinary = true,
        }));
}
