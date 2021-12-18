using System.Collections.Generic;

namespace Hast.Layer
{
    /// <summary>
    /// Represents the implemented hardware that was created from the hardware description generated based on the
    /// original .NET assemblies.
    /// </summary>
    public interface IHardwareRepresentation
    {
        /// <summary>
        /// Gets the original assemblies' paths this hardware assembly was generated from.
        /// </summary>
        IEnumerable<string> SoftAssemblyPaths { get; }

        /// <summary>
        /// Gets the hardware created from a transformed assembly.
        /// </summary>
        IHardwareDescription HardwareDescription { get; }

        /// <summary>
        /// Gets a handle to the hardware implementation synthesized through the FPGA vendor tool-chain.
        /// </summary>
        IHardwareImplementation HardwareImplementation { get; }

        /// <summary>
        /// Gets the capabilities, like available memory, of the hardware device the representation was created for.
        /// </summary>
        IDeviceManifest DeviceManifest { get; }

        /// <summary>
        /// Gets the configuration used to create this representation.
        /// </summary>
        IHardwareGenerationConfiguration HardwareGenerationConfiguration { get; }
    }
}
