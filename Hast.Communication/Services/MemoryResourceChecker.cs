using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;

namespace Hast.Communication.Services
{
    public class MemoryResourceChecker : IMemoryResourceChecker
    {
        public void EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation)
        {
            var availableBytes = hardwareRepresentation.DeviceManifest.AvailableMemoryBytes;

            var memoryByteCount = (ulong)memory.ByteCount;
            if (memoryByteCount > availableBytes)
            {
                throw new InvalidOperationException(
                    $"The input is too large to fit into the device's memory: The input is {memoryByteCount} bytes, " +
                    $"the available memory is {availableBytes} bytes.");
            }
        }
    }
}
