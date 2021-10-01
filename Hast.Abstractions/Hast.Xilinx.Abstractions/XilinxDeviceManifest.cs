using Hast.Synthesis.Abstractions;
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
        /// Gets or sets the collection of supported platform names (can be partial, wildcard is considered at the end).
        /// The full platform name is the name of the directory in $XILINX_PLATFORM (defaults to "$XILINX_XRT/platforms"
        /// if not set) where that directory contains an .xpfm file.
        /// </summary>
        public IList<string> SupportedPlatforms { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the binary should be netlist (DCP) format instead of the default
        /// bitstream.
        /// </summary>
        public bool RequiresDcpBinary { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="DeviceManifest.ClockFrequencyHz"/> can be passed to the
        /// device during hardware implementation composition. This is ignored by the Nexys devices.
        /// </summary>
        public bool BuildWithClockFrequencyHz { get; set; } = true;

        /// <summary>
        /// Gets or sets the bandwidth of the Advanced eXtensible Interface (AXI) bus in bits. This is ignored by the
        /// Nexys devices.
        /// </summary>
        public int AxiBusWith { get; set; } = 512;
    }
}
