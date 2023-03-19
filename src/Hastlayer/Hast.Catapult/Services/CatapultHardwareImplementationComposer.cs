using Hast.Catapult.Models;
using Hast.Layer;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using System.Threading.Tasks;

namespace Hast.Catapult.Services;

public class CatapultHardwareImplementationComposer : IHardwareImplementationComposer
{
    public bool CanCompose(IHardwareImplementationCompositionContext context) =>
        context.DeviceManifest.Name == CatapultManifestProvider.DeviceName;

    public Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context) =>
        // Not yet implemented, just here as a placeholder.
        Task.FromResult((IHardwareImplementation)new HardwareImplementation());
}
