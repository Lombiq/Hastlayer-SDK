namespace Hast.Xilinx.ManifestProviders;

public class ZedBoardManifestProvider : ZynqManifestProviderBase
{
    public const string DeviceName = "ZedBoard";

    public ZedBoardManifestProvider() => _deviceName = DeviceName;
}
