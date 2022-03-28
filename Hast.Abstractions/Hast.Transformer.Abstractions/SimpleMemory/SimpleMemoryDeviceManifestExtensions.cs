using Hast.Layer;

namespace Hast.Transformer.Abstractions.SimpleMemory;

public static class SimpleMemoryDeviceManifestExtensions
{
    public static ulong GetAvailableSimpleMemoryCellCount(this IDeviceManifest deviceManifest) =>
        deviceManifest.AvailableMemoryBytes / SimpleMemory.MemoryCellSizeBytes;
}
