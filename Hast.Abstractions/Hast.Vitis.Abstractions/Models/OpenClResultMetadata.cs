using Hast.Vitis.Abstractions.Constants;
using System;
using System.Runtime.InteropServices;

using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;

namespace Hast.Vitis.Abstractions.Models;

public class OpenClResultMetadata
{
    public ulong ExecutionTime { get; }

    public OpenClResultMetadata(Span<byte> hostBufferSpan, bool isBigEndian)
    {
        var executionTimeSpan = hostBufferSpan.Slice(HeaderOffsets.ExecutionTime, 2 * MemoryCellSizeBytes);

        // Swap the two cells if host and device endianness don't match.
        if (isBigEndian == BitConverter.IsLittleEndian)
        {
            Span<byte> temp = new byte[MemoryCellSizeBytes];
            executionTimeSpan.Slice(0, MemoryCellSizeBytes).CopyTo(temp);
            executionTimeSpan[MemoryCellSizeBytes..].CopyTo(executionTimeSpan);
            temp.CopyTo(executionTimeSpan[MemoryCellSizeBytes..]);
        }

        ExecutionTime = MemoryMarshal.Read<ulong>(executionTimeSpan);
    }
}
