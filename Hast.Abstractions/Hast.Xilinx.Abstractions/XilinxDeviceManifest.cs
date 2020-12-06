#nullable enable
using Hast.Synthesis.Abstractions;
using System.Collections.Generic;

namespace Hast.Xilinx.Abstractions
{
    public class XilinxDeviceManifest : DeviceManifest
    {
        /// <summary>
        /// Gets or sets a value indicating whether High Bandwidth Memory is available on this device.
        /// </summary>
        public bool SupportsHbm { get; set; }

        /// <summary>
        /// Gets the collection of supported platform names (can be partial, wildcard is considered at the end).
        /// The full platform name is the name of the directory in $XILINX_PLATFORM (defaults to "$XILINX_XRT/platforms"
        /// if not set) where that directory contains an .xpfm file.
        /// </summary>
        public IList<string> SupportedPlatforms { get; }

        public XilinxDeviceManifest(bool supportsHbm, IList<string>? supportedPlatforms)
        {
            SupportsHbm = supportsHbm;
            SupportedPlatforms = supportedPlatforms ?? new List<string>();
        }

        public XilinxDeviceManifest(bool supportsHbm = true, params string[] supportedPlatforms)
        {
            SupportsHbm = supportsHbm;
            SupportedPlatforms = supportedPlatforms;
        }
    }
}
