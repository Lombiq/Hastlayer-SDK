using System;

// This is so the Memory property can be read when handling communication with the FPGA but not by user code.
// This could be supposedly also in AssemblyInfo.cs but there it doesn't work.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Hast.Communication")]

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
        public const uint MemoryCellSizeBytes = 4;

        /// <summary>
        /// Gets or sets the contents of the memory representation.
        /// </summary>
        /// <remarks>
        /// This is internal so the property can be read when handling communication with the FPGA but not by user code.
        /// </remarks>
        internal byte[] Memory { get; set; }

        /// <summary>
        /// Gets the number of cells of this memory allocation, indicating memory cells of size <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        public int CellCount { get; private set; }


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
            CellCount = cellCount;
        }


        public void Write4Bytes(int cellIndex, byte[] input)
        {
            if (input.Length > MemoryCellSizeBytes)
            {
                throw new ArgumentException("The byte array to be written to memory should be shorter than " + MemoryCellSizeBytes + ".");
            }

            for (uint i = 0; i < input.Length; i++)
            {
                Memory[i + cellIndex * MemoryCellSizeBytes] = input[i];
            }

            for (uint i = (uint)input.Length; i < MemoryCellSizeBytes; i++)
            {
                Memory[i + cellIndex * MemoryCellSizeBytes] = 0;
            }
        }

        public byte[] Read4Bytes(int cellIndex)
        {
            var output = new byte[MemoryCellSizeBytes];

            for (uint i = 0; i < MemoryCellSizeBytes; i++)
            {
                output[i] = Memory[i + cellIndex * MemoryCellSizeBytes];
            }

            return output;
        }

        public void WriteUInt32(int cellIndex, uint number) => Write4Bytes(cellIndex, BitConverter.GetBytes(number));

        public uint ReadUInt32(int cellIndex) =>BitConverter.ToUInt32(Read4Bytes(cellIndex), 0);

        public void WriteInt32(int cellIndex, int number) => Write4Bytes(cellIndex, BitConverter.GetBytes(number));

        public int ReadInt32(int cellIndex) => BitConverter.ToInt32(Read4Bytes(cellIndex), 0);

        public void WriteBoolean(int cellIndex, bool boolean) =>
            // Since the implementation of a boolean can depend on the system rather hard-coding the expected values here
            // so on the FPGA-side we can depend on it.
            Write4Bytes(cellIndex, boolean ? new byte[] { 255, 255, 255, 255 } : new byte[] { 0, 0, 0, 0 });

        public bool ReadBoolean(int cellIndex)
        {
            var bytes = Read4Bytes(cellIndex);
            return bytes[0] != 0 || bytes[1] != 0 || bytes[2] != 0 || bytes[3] != 0;
        }
    }
}
