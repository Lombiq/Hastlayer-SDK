using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Communication.Services
{
    public class MemoryResourceChecker : IMemoryResourceChecker
    {
        public MemoryResourceProblem EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation)
        {
            var availableBytes = hardwareRepresentation.DeviceManifest.AvailableMemoryBytes;

            var memoryByteCount = (ulong)memory.ByteCount;
            if (memoryByteCount > availableBytes)
            {
                return new MemoryResourceProblem
                {
                    Sender = this,
                    Message = string.Empty,
                    AvailableByteCount = availableBytes,
                    MemoryByteCount = memoryByteCount,
                };
            }

            return null;
        }
    }
}
