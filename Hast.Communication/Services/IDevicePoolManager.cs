using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Orchard;

namespace Hast.Communication.Services
{
    /// <summary>
    /// Manages the usage of the pool of connected compatible devices to make maximal utilization possible.
    /// </summary>
    public interface IDevicePoolManager : ISingletonDependency
    {
        /// <summary>
        /// Sets the current device pool to contain the give devices. It overwrites the previous pool completely.
        /// </summary>
        /// <param name="devices">The devices to put into the pool.</param>
        void SetDevicePool(IEnumerable<IDevice> devices);

        /// <summary>
        /// Gets the devices contained in the current device pool.
        /// </summary>
        /// <returns>The devices in the current device pool.</returns>
        IEnumerable<IPooledDevice> GetDevicesInPool();

        /// <summary>
        /// Reserves an available device from the pool.
        /// </summary>
        /// <returns>A <c>Task</c> that will complete once an available device could be reserved from the pool.</returns>
        Task<IReservedDevice> ReserveDevice();
    }
}
