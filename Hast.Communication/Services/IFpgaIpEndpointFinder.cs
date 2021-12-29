using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Common.Interfaces;
using Hast.Communication.Models;

namespace Hast.Communication.Services
{
    /// <summary>
    /// Service for finding FPGA endpoints connected to the available networks of this computer.
    /// </summary>
    public interface IFpgaIpEndpointFinder : IDependency
    {
        /// <summary>
        /// Returns all FPGA endpoints connected to the available networks of this computer.
        /// </summary>
        /// <returns>FPGA endpoints connected to the available networks of this computer.</returns>
        Task<IEnumerable<IFpgaEndpoint>> FindFpgaEndpointsAsync();
    }
}
