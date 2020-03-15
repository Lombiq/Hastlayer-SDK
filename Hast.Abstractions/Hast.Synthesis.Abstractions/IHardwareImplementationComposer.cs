using Hast.Layer;
using Orchard;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationComposer : IDependency
    {
        /// <summary>
        /// Composes the hardware implementation for the given hardware representation.
        /// </summary>
        /// <param name="configuration">Configuration for how the hardware generation should happen.</param>
        /// <param name="hardwareDescription">Represents the hardware that was generated from .NET assemblies.</param>
        /// <param name="deviceManifest">The hardware device's manifest to compose the hardware implementation for.</param>
        /// <returns>The handle to the synthesized hardware implementation.</returns>
        Task<IHardwareImplementation> Compose(
            IHardwareGenerationConfiguration configuration,
            IHardwareDescription hardwareDescription,
            IDeviceManifest deviceManifest);
    }
}
