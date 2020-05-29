using System;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    [Obsolete("For internal use only.")]
    public static class SimpleMemoryConfigurator
    {
        public static void UpdateAlignment(int value) => SimpleMemory.Alignment = value;
    }
}
