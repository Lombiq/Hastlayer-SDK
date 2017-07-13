using Hast.Layer;
using Orchard;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestProvider : IDependency
    {
        IDeviceManifest DeviceManifest { get; }
    }
}
