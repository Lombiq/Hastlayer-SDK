using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    public static class SimpleMemoryDeviceManifestExtensions
    {
        public static uint GetAvailableSimpleMemoryCellCount(this IDeviceManifest deviceManifest) =>
            deviceManifest.AvailableMemoryBytes / SimpleMemory.MemoryCellSizeBytes;
    }
}
