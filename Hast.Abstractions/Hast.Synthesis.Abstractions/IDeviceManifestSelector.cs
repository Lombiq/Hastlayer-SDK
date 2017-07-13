using System.Collections.Generic;
using Hast.Layer;
using Orchard;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestSelector : IDependency
    {
        IEnumerable<IDeviceManifest> GetSupporteDevices();
    }
}
