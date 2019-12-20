using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Orchard;

namespace Hast.Communication.Services
{
    /// <summary>
    /// Service for populating the device pool with device handles.
    /// </summary>
    public interface IDevicePoolPopulator : ISingletonDependency
    {
        /// <summary>
        /// Populates the device pool with the given device handles.
        /// </summary>
        /// <param name="devicesFactory">
        /// The factory function to produce the device. Will only be run if the device pool wasn't previously populated.
        /// </param>
        void PopulateDevicePoolIfNew(Func<Task<IEnumerable<IDevice>>> devicesFactory);
    }
}
