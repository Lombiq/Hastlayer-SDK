namespace Hast.Common.Enums;

/// <summary>
/// The usage flavor of Hastlayer for different scenarios.
/// </summary>
public enum HastlayerFlavor
{
    /// <summary>
    /// Flavor for the developers of Hastlayer itself. It includes the full source code.
    /// </summary>
    Developer,

    /// <summary>
    /// Flavor for end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service.
    /// </summary>
    Client,
}
