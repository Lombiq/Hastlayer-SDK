using Hast.Layer;
using Hast.Synthesis.Abstractions;

namespace Hast.Xilinx.Abstractions
{
    public class Nexys4DdrManifestProvider : NexysManifestProviderBase
    {
        public const string DeviceName = "Nexys4 DDR";

        static Nexys4DdrManifestProvider() => DeviceNameInternal = DeviceName;
    }
}
