using Hast.Common.Enums;
using Hast.Common.Interfaces;

namespace Hast.Vitis.Abstractions.Models;

public class VitisBuildConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether only run Vivado synthesis to generate a report, don't build via v++.
    /// Exits after synthesis is complete.
    /// </summary>
    public bool SynthesisOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device will be reset via <c>xbutil reset</c> before the first build.
    /// </summary>
    public bool ResetOnFirstRun { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the composer will pause after saving the vhd files to disk and before
    /// checking if the xclbin files exist. This gives the user opportunity to perform manual build and create their own
    /// binaries to the location where Hastlayer normally looks for it.
    /// </summary>
    /// <remarks>
    /// <para>It prompts via the command line so this is not suitable for GUI applications.</para>
    /// <para>
    /// This setting is ignored in the <see cref="IHastlayerFlavorProvider.Flavor"/> is <see
    /// cref="HastlayerFlavor.Client"/>.
    /// </para>
    /// </remarks>
    public bool PromptBeforeBuild { get; set; }
}
