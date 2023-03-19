using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Xilinx.Abstractions;

public class XilinxDeviceManifest : DeviceManifest
{
}

public class NexysDeviceManifest : XilinxDeviceManifest
{
}

public class VitisDeviceManifest : XilinxDeviceManifest
{
    /// <summary>
    /// Gets or sets a value indicating whether High Bandwidth Memory is available on this device.
    /// </summary>
    public bool SupportsHbm { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of supported platform names (can be partial, wildcard is considered at the end). The
    /// full platform name is the name of the directory in $XILINX_PLATFORM (defaults to "$XILINX_XRT/platforms" if not
    /// set) where that directory contains an .xpfm file.
    /// </summary>
    [SuppressMessage(
        "Usage",
        "CA2227:Collection properties should be read only",
        Justification = "These aren't changed during run so it doesn't matter but it'd add unnecesary complexity.")]
    public IList<string> SupportedPlatforms { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the binary should be netlist (DCP) format instead of the default
    /// bitstream.
    /// </summary>
    public bool RequiresDcpBinary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeviceManifest.ClockFrequencyHz"/> can be passed to the
    /// device during hardware implementation composition.
    /// </summary>
    public bool BuildWithClockFrequencyHz { get; set; } = true;

    /// <summary>
    /// Gets or sets the bandwidth of the Advanced eXtensible Interface (AXI) bus in bits.
    /// </summary>
    public int AxiBusWith { get; set; } = 512;
}

public class AzureNpDeviceManifest : VitisDeviceManifest
{
}

public class ZynqDeviceManifest : VitisDeviceManifest
{
}
