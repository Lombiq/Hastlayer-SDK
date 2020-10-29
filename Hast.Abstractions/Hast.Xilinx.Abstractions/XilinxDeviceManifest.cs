using Hast.Synthesis.Abstractions;
using System;

namespace Hast.Xilinx.Abstractions
{
    public class XilinxDeviceManifest : DeviceManifest
    {
        /// <summary>
        /// Gets or sets a value indicating whether High Bandwidth Memory is available on this device.
        /// </summary>
        public bool SupportsHbm { get; set; } = true;

        /// <summary>
        /// Gets or sets hardware family.
        /// </summary>
        public XilinxDeviceType DeviceType { get; set; } = XilinxDeviceType.Vitis;

        /// <summary>
        /// Gets or sets the partial name of the platform directory that contains the .xpfm file.
        /// </summary>
        public string PlatformName { get; set; }


        public XilinxDeviceManifest() => ToolChainName = CommonToolChainNames.Vivado;


        public Exception GetUnknownDeviceType() =>
            new InvalidOperationException($"Unknown device type: {DeviceType}");
    }
}
