using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationCompositionContext
    {
        /// <summary>
        /// Gets the configuration for how the hardware generation should happen.
        /// </summary>
        IHardwareGenerationConfiguration Configuration { get; }

        /// <summary>
        /// Gets the hardware device's manifest to compose the hardware implementation for.
        /// </summary>
        IDeviceManifest DeviceManifest { get; }

        /// <summary>
        /// Gets the hardware description that was generated from .NET assemblies.
        /// </summary>
        IHardwareDescription HardwareDescription { get; }
    }
}
