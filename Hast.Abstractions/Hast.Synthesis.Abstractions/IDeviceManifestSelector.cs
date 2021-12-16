using System.Collections.Generic;
using Hast.Common.Interfaces;
using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    /// <summary>
    /// Service for retrieving device manifests.
    /// </summary>
    public interface IDeviceManifestSelector : IDependency
    {
        /// <summary>
        /// Returns the available <see cref="IDeviceManifest"/> instances.
        /// </summary>
        IEnumerable<IDeviceManifest> GetSupportedDevices();
    }
}
