namespace Hast.Layer
{
    /// <summary>
    /// Represents a handle to the hardware implementation synthesized through the FPGA vendor tool-chain.
    /// </summary>
    public interface IHardwareImplementation
    {
        /// <summary>
        /// Gets the path of the binary to be executed on hardware, if any.
        /// </summary>
        string BinaryPath { get; }
    }
}
