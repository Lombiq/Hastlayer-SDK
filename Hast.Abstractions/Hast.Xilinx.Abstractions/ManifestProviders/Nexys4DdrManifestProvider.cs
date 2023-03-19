namespace Hast.Xilinx.ManifestProviders;

public class Nexys4DdrManifestProvider : NexysManifestProviderBase
{
    public const string DeviceName = "Nexys4 DDR";

    public Nexys4DdrManifestProvider() => _deviceName = DeviceName;
}
