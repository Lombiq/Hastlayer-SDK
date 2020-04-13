using System.Collections.Generic;
using Hast.Common.Interfaces;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public interface IDeviceManifestSelector : IDependency
    {
        IEnumerable<IDeviceManifest> GetSupportedDevices();
    }
}
