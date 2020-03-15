using Hast.Layer;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public class NullHardwareImplementationComposer : IHardwareImplementationComposer
    {
        public Task<IHardwareImplementation> Compose(
            IHardwareGenerationConfiguration configuration,
            IHardwareDescription hardwareDescription,
            IDeviceManifest deviceManifest)
        {
            // Not yet implemented, just here as a placeholder.
            return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
        }


        public class HardwareImplementation : IHardwareImplementation
        {
        }
    }
}
