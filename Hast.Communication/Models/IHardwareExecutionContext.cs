using Hast.Layer;

namespace Hast.Communication.Models
{
    /// <summary>
    /// The configuration and data required for execution on hardware.
    /// </summary>
    public interface IHardwareExecutionContext
    {
        /// <summary>
        /// Gets the configuration for <c>IHastlayer.GenerateProxyAsync</c>.
        /// </summary>
        IProxyGenerationConfiguration ProxyGenerationConfiguration { get; }

        /// <summary>
        /// Gets the hardware implementation and context of the transformed .Net code.
        /// </summary>
        IHardwareRepresentation HardwareRepresentation { get; }
    }
}
