using System.Collections.Generic;

namespace Hast.Layer
{
    /// <summary>
    /// Represents a handle to the hardware implementation synthesized through the FPGA vendor toolchain.
    /// </summary>
    public interface IHardwareImplementation
    {
        /// <summary>
        /// Gets or sets the path of the binary to be executed on hardware, if any.
        /// </summary>
        string BinaryPath { get; set; }

        /// <summary>
        /// Gets the utilization percentage values for hardware resources where that information is available.
        /// </summary>
        public IDictionary<string, decimal> ResourceUsagePercent { get; }

        /// <summary>
        /// Gets or sets the wattage of the hardware design.
        /// </summary>
        public decimal PowerUsageWatts { get; set; }
    }
}
