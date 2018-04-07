using Hast.Layer;
using Orchard;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestProvider : ISingletonDependency
    {
        IDeviceManifest DeviceManifest { get; }
    }
}
