using System;
using System.Diagnostics.CodeAnalysis;
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
        public const int DefaultPrefixCellCount = 4;

        /// <summary>
        /// The alignment value, which must be an integer power of 2.
        /// </summary>
        public static int Alignment { get; set; }

        /// <summary>
        /// The number of extra cells used for header information like memberId or data length.
        /// </summary>
        public int PrefixCellCount { get; internal set; } = DefaultPrefixCellCount;

        /// <summary>
        /// This is the full memory including the <see cref="PrefixCellCount"/> cells of extra memory that is to be
        /// used for passing in extra input parameters (like memberId) without having to copy the operative memory
        /// contents into an auxiliary array.
        /// </summary>
        internal Memory<byte> PrefixedMemory { get; set; }

        /// <summary>
        /// Gets or sets the contents of the memory representation.
        /// </summary>
        /// <remarks>
        /// This is internal so the property can be read when handling communication with the FPGA but not by user code.
        /// </remarks>
        internal Memory<byte> Memory => PrefixedMemory.Slice(PrefixCellCount * MemoryCellSizeBytes);

        /// <summary>
        /// Gets the number of bytes of this memory allocation.
        /// </summary>
        public int ByteCount => Memory.Length;

        /// <summary>
        /// Gets the number of cells of this memory allocation, indicating memory cells of size <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        public int CellCount => Memory.Length / MemoryCellSizeBytes;

        /// <summary>
        /// Gets the span of memory at the cellIndex, the length is <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        /// <param name="cellIndex">The cell index where the memory span starts.</param>
        /// <returns>A span starting at cellIndex * MemoryCellSizeBytes.</returns>
        private Span<byte> this[int cellIndex]
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
        public SimpleMemory(int cellCount) : this(new byte[(cellCount + DefaultPrefixCellCount + Alignment) *
                                                           MemoryCellSizeBytes], DefaultPrefixCellCount) { }

        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on
        /// the FPGA for transformed algorithms from an existing byte array.
        /// </summary>
        /// <param name="memory">The source data.</param>
        /// <param name="prefixCellCount">The amount of cells for header data. See <see cref="PrefixCellCount"/>.</param>
        /// <remarks>
        /// This constructor is internal only to avoid dependency issues where we have to include System.Memory package
        /// everywhere where SimpleMemory is used even if it's created with the other constructors. Instead, you can use
        /// <see cref="SimpleMemoryAccessor.Create(Memory{byte}, int)"/> to construct a <see cref="SimpleMemory"/> from
        /// <see cref="Memory{byte}"/>.
        /// </remarks>
        internal SimpleMemory(Memory<byte> memory, int prefixCellCount)
        {
            if (Alignment > 0)
            {
                IntPtr address;
                unsafe
                {
                    fixed (byte* pointer = memory.Span)
                    {
                        address = new IntPtr(pointer);
                    }
                }

                var alignmentOffset = Alignment - (int)(address.ToInt64() % Alignment);
                memory = memory.Slice(alignmentOffset);
            }

            PrefixedMemory = memory;
            PrefixCellCount = prefixCellCount;
        }


        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on
        /// the FPGA for transformed algorithms from an existing byte array.
        /// </summary>
        /// <param name="memory">The source data.</param>
        /// <param name="prefixCellCount">The amount of cells for header data. See <see cref="PrefixCellCount"/>.</param>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by third-party consumer code.")]
        private SimpleMemory(byte[] memory, int prefixCellCount = 0) : this(memory.AsMemory(), prefixCellCount) { }


        public void Write4Bytes(int cellIndex, byte[] bytes)
        {
            var target = this[cellIndex];

            for (int i = 0; i < bytes.Length; i++) target[i] = bytes[i];
            for (int i = bytes.Length; i < MemoryCellSizeBytes; i++) target[i] = 0;
        }

        public byte[] Read4Bytes(int cellIndex)
        {
            var output = new byte[MemoryCellSizeBytes];
            this[cellIndex].CopyTo(output);
            return output;
        }

        public void WriteUInt32(int cellIndex, uint number) => MemoryMarshal.Write(this[cellIndex], ref number);

        public uint ReadUInt32(int cellIndex) => MemoryMarshal.Read<uint>(this[cellIndex]);

        public void WriteInt32(int cellIndex, int number) => Write4Bytes(cellIndex, BitConverter.GetBytes(number));

        public int ReadInt32(int cellIndex) => MemoryMarshal.Read<int>(this[cellIndex]);

        public void WriteBoolean(int cellIndex, bool boolean) =>
            // Since the implementation of a boolean can depend on the system rather hard-coding the expected values
            // here so on the FPGA-side we can depend on it. Can't call MemoryMarshal.Write directly because its second
            // parameter must be passed using "ref" and you can't pass in constants or expressions by reference.
            WriteUInt32(cellIndex, boolean ? uint.MaxValue : uint.MinValue);

        public bool ReadBoolean(int cellIndex) => MemoryMarshal.Read<uint>(this[cellIndex]) != uint.MinValue;
    }


    /// <summary>
    /// Extensions for older Framework features which don't support Memory or Span yet.
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>
        /// Gets the internal array to be used for <see cref="System.IO.Stream.Write(byte[], int, int)"/>.
        /// </summary>
        /// <param name="bytes">The source.</param>
        /// <returns>The underlying array.</returns>
        /// <remarks>
        /// Once Stream.Read(Span) based overload is available, use that instead!
        /// https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.read?view=netcore-2.2#System_IO_Stream_Read_System_Span_System_Byte__
        /// </remarks>
        public static ArraySegment<byte> GetUnderlyingArray(this Memory<byte> bytes) => GetUnderlyingArray((ReadOnlyMemory<byte>)bytes);

        public static ArraySegment<byte> GetUnderlyingArray(this ReadOnlyMemory<byte> bytes)
        {
            if (!MemoryMarshal.TryGetArray(bytes, out var arraySegment)) throw new NotSupportedException("This Memory does not support exposing the underlying array.");
            return arraySegment;
        }
    }
}
