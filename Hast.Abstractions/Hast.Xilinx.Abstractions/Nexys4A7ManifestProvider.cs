namespace Hast.Xilinx.Abstractions
{
    public class NexysA7ManifestProvider : NexysManifestProviderBase
    {
        public const string DeviceName = "Nexys A7";


        static NexysA7ManifestProvider() => DeviceNameInternal = DeviceName;
    }
}
