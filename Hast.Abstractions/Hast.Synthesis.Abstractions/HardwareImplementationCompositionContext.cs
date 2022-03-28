using Hast.Layer;

namespace Hast.Synthesis.Abstractions;

public class HardwareImplementationCompositionContext : IHardwareImplementationCompositionContext
{
    public IHardwareGenerationConfiguration Configuration { get; set; }
    public IHardwareDescription HardwareDescription { get; set; }
    public IDeviceManifest DeviceManifest { get; set; }
}
