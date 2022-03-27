using System.Runtime.InteropServices;

namespace System;

public static class SimpleMemoryExtensions
{
    public static void SetIntegers(this Span<byte> buffer, int startIndex, params int[] values)
    {
        for (int i = 0, index = startIndex; i < values.Length; i++, index += sizeof(int))
        {
            var slide = buffer.Slice(index, sizeof(int));
            MemoryMarshal.Write(slide, ref values[i]);
        }
    }
}
