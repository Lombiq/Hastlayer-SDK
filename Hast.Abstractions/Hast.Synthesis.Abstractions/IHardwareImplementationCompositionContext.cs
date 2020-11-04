using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationCompositionContext
    {
        /// <summary>
        /// Configuration for how the hardware generation should happen.
        /// </summary>
        IHardwareGenerationConfiguration Configuration { get; }

        /// <summary>
        /// The hardware device's manifest to compose the hardware implementation for.
        /// </summary>
        IDeviceManifest DeviceManifest { get; }

        /// <summary>
        /// Represents the hardware that was generated from .NET assemblies.
        /// </summary>
        IHardwareDescription HardwareDescription { get; }
    }
}
