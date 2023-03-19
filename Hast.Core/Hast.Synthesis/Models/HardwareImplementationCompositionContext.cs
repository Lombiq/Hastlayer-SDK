using Hast.Layer;

namespace Hast.Synthesis.Models;

public class HardwareImplementationCompositionContext : IHardwareImplementationCompositionContext
{
    public IHardwareGenerationConfiguration Configuration { get; set; }
    public IHardwareDescription HardwareDescription { get; set; }
    public IDeviceManifest DeviceManifest { get; set; }
}
