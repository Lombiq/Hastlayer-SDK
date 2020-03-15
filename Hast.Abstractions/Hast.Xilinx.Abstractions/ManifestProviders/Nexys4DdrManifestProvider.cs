namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public class Nexys4DdrManifestProvider : NexysManifestProviderBase
    {
        public const string DeviceName = "Nexys4 DDR";

        static Nexys4DdrManifestProvider() => DeviceNameInternal = DeviceName;
    }
}
