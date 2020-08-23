using Hast.Synthesis.Abstractions;

namespace Hast.Xilinx.Abstractions
{
    public class XilinxDeviceManifest : DeviceManifest
    {
        /// <summary>
        /// Gets or sets if the High Bandwidth Memory is available on this device.
        /// </summary>
        public bool SupportsHbm { get; set; } = true;
    }
}
