using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public class NullHardwareImplementationComposer : IHardwareImplementationComposer
    {
        public Task<IHardwareImplementation> Compose(IHardwareDescription hardwareDescription)
        {
            // Not yet implemented, just here as a placeholder.
            return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
        }


        public class HardwareImplementation : IHardwareImplementation
        {
        }
    }
}
