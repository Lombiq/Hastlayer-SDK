using Hast.Common.Enums;

namespace Hast.Common.Interfaces;

/// <summary>
/// An interface for any class that implements <see cref="HastlayerFlavor"/> (see
/// <c>Hast.Layer.IHastlayerConfiguration</c>).
/// </summary>
public interface IHastlayerFlavorProvider
{
    /// <summary>
    /// Gets the usage flavor of Hastlayer for different scenarios. Defaults to <see cref="HastlayerFlavor.Client"/> in
    /// the client SDK and <see cref="HastlayerFlavor.Developer"/> otherwise.
    /// </summary>
    HastlayerFlavor Flavor { get; }
}
