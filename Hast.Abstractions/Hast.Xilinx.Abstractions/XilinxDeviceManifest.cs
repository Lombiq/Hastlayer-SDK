using Hast.Synthesis.Abstractions;
using System;
using System.Collections.Generic;

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
        /// Gets or sets the collection of supported platform names (can be partial, wildcard is considered at the end).
        /// The full platform name is the name of the directory in $XILINX_PLATFORM (defaults to "$XILINX_XRT/platforms"
        /// if not set) where that directory contains an .xpfm file.
        /// </summary>
        public IList<string> SupportedPlatforms { get; set; }


        public XilinxDeviceManifest() => ToolChainName = CommonToolChainNames.Vivado;


        public Exception GetUnknownDeviceType() =>
            new InvalidOperationException($"Unknown device type: {DeviceType}");
    }
}
