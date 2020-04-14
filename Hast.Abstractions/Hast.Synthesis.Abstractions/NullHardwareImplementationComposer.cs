using Hast.Layer;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public class NullHardwareImplementationComposer : IHardwareImplementationComposer
    {
        public bool CanCompose(IHardwareImplementationCompositionContext context) => true;

        public Task<IHardwareImplementation> Compose(IHardwareImplementationCompositionContext context) =>
            Task.FromResult((IHardwareImplementation)new HardwareImplementation());
    }
}
