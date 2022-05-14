using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Vitis.Abstractions.Constants;

public static class HeaderOffsets
{
    public const int HeaderCellCount = 0 * SimpleMemory.MemoryCellSizeBytes;
    public const int MemberId = 1 * SimpleMemory.MemoryCellSizeBytes;
    public const int ExecutionTime = 2 * SimpleMemory.MemoryCellSizeBytes;
}
