namespace Hast.Xilinx.Abstractions.ManifestProviders
{
    public class NexysA7ManifestProvider : NexysManifestProviderBase
    {
        public const string DeviceName = "Nexys A7";


        public NexysA7ManifestProvider()
        {
            _deviceName = DeviceName;
        }
    }
}
