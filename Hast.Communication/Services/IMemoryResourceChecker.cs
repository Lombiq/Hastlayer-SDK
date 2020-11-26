using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;

namespace Hast.Communication.Services
{
    /// <summary>
    /// Checks if the memory resources can fit the <see cref="SimpleMemory"/> before sending it to the device.
    /// </summary>
    public interface IMemoryResourceChecker
    {
        /// <summary>
        /// Verifies resource availability. If there is an issue, an exception is thrown.
        /// </summary>
        /// <param name="memory">The memory we want to send to the device.</param>
        /// <param name="hardwareRepresentation">The representation of the device and program on it.</param>
        /// <exception cref="InvalidOperationException">When there are not enough resources.</exception>
        void EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation);
    }
}
