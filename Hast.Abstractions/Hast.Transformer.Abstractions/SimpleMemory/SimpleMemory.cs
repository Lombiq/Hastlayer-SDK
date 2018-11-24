using System;
using System.Runtime.InteropServices;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    /// <summary>
    /// Represents a simplified memory model available on the FPGA for transformed algorithms. WARNING: SimpleMemory is
    /// NOT thread-safe, there shouldn't be any concurrent access to it.
    /// </summary>
    /// <remarks>
    /// All read/write methods' name MUST follow the convention to begin with "Read" or "Write" respectively.
    /// 
    /// WARNING: changes here should be in sync with the VHDL library. The signatures of the methods of this class 
    /// mustn't be changed (i.e. no renames, new or re-ordered arguments) without making adequate changes to the VHDL
    /// library too.
    /// </remarks>
    public class SimpleMemory
    {
        public const int MemoryCellSizeBytes = sizeof(int);


        /// <summary>
        /// Gets or sets the contents of the memory representation.
        /// </summary>
        /// <remarks>
        /// This is internal so the property can be read when handling communication with the FPGA but not by user code.
        /// </remarks>
        internal Memory<byte> Memory { get; set; }

        /// <summary>
        /// Gets the number of cells of this memory allocation, indicating memory cells of size <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        public int CellCount { get => Memory.Length / MemoryCellSizeBytes; }

        /// <summary>
        /// Gets the span of memory at the cellIndex, the length is <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        /// <param name="cellIndex">The cell index where the memory span starts.</param>
        /// <returns>A span starting at cellIndex * MemoryCellSizeBytes.</returns>
        public Span<byte> this[int cellIndex]
        {
            get => Memory.Slice(cellIndex * MemoryCellSizeBytes, MemoryCellSizeBytes).Span;
        }

        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on 
        /// the FPGA for transformed algorithms.
        /// </summary>
        /// <param name="cellCount">
        /// The number of memory "cells". The memory is divided into independently accessible "cells"; the size of the
        /// allocated memory space is calculated from the cell count and the cell size indicated in 
        /// <see cref="MemoryCellSizeBytes"/>.
        /// </param>
        public SimpleMemory(int cellCount)
        {
            Memory = new byte[cellCount * MemoryCellSizeBytes];
        }

        public void Write4Bytes(int cellIndex, Span<byte> bytes)
        {
            var target = this[cellIndex];

            for (int i = 0; i < bytes.Length; i++) target[i] = bytes[i];
            for (int i = bytes.Length; i < MemoryCellSizeBytes; i++) target[i] = 0;
        }

        public void Write4Bytes(int startCellIndex, params byte[][] bytesMatrix)
        {
            for (int i = 0; i < bytesMatrix.Length; i++)
                Write4Bytes(startCellIndex + i, bytesMatrix[i]);
        }

        [Obsolete("Use the indexer to get the span of this the 4 bytes at this cell index.")]
        public byte[] Read4Bytes(int cellIndex)
        {
            var output = new byte[MemoryCellSizeBytes];

            this[cellIndex].CopyTo(output);

            return output;
        }

        public Memory<byte>[] Read4Bytes(int startCellIndex, int count)
        {
            var matrix = new Memory<byte>[count];
            int offset = startCellIndex * MemoryCellSizeBytes;
            for (int i = 0; i < count; i++) matrix[i] = Memory.Slice(offset + i * MemoryCellSizeBytes, MemoryCellSizeBytes);
            return matrix;
        }

        public void WriteUInt32(int cellIndex, uint number) => MemoryMarshal.Write(this[cellIndex], ref number);

        public void WriteUInt32(int startCellIndex, params uint[] numbers) =>
            MemoryMarshal.Cast<uint, byte>(numbers)
                .CopyTo(Memory.Slice(startCellIndex * MemoryCellSizeBytes, sizeof(uint)).Span);

        public uint ReadUInt32(int cellIndex) => MemoryMarshal.Read<uint>(this[cellIndex]);

        public uint[] ReadUInt32(int startCellIndex, int count)
        {
            var numbers = new uint[count];

            for (int i = 0; i < count; i++)
            {
                numbers[i] = ReadUInt32(startCellIndex + i);
            }

            return numbers;
        }

        public void WriteInt32(int cellIndex, int number) => Write4Bytes(cellIndex, BitConverter.GetBytes(number));

        public void WriteInt32(int startCellIndex, params int[] numbers)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                WriteInt32(startCellIndex + i, numbers[i]);
            }
        }

        public int ReadInt32(int cellIndex) => MemoryMarshal.Read<int>(this[cellIndex]);

        public int[] ReadInt32(int startCellIndex, int count)
        {
            var numbers = new int[count];

            for (int i = 0; i < count; i++)
            {
                numbers[i] = ReadInt32(startCellIndex + i);
            }

            return numbers;
        }

        public void WriteBoolean(int cellIndex, bool boolean) =>
            // Since the implementation of a boolean can depend on the system rather hard-coding the expected values here
            // so on the FPGA-side we can depend on it.
            Write4Bytes(cellIndex, boolean ? new byte[] { 255, 255, 255, 255 } : new byte[] { 0, 0, 0, 0 });

        public void WriteBoolean(int startCellIndex, params bool[] booleans)
        {
            for (int i = 0; i < booleans.Length; i++)
            {
                WriteBoolean(startCellIndex + i, booleans[i]);
            }
        }

        public bool ReadBoolean(int cellIndex) => MemoryMarshal.Read<int>(this[cellIndex]) != 0;

        public bool[] ReadBoolean(int startCellIndex, int count)
        {
            var booleans = new bool[count];

            for (int i = 0; i < count; i++)
            {
                booleans[i] = ReadBoolean(startCellIndex + i);
            }

            return booleans;
        }
    }
}
