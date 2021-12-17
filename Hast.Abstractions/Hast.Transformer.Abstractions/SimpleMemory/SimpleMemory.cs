using Hast.Synthesis.Abstractions;
using Microsoft.Extensions.Logging;
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
        /// Gets the span of memory at the cellIndex, the length is <see cref="MemoryCellSizeBytes"/>.
        /// </summary>
        /// <param name="cellIndex">The cell index where the memory span starts.</param>
        /// <returns>A span starting at cellIndex * MemoryCellSizeBytes.</returns>
        private Span<byte> this[int cellIndex] => Memory.Slice(cellIndex * MemoryCellSizeBytes, MemoryCellSizeBytes).Span;

        /// <summary>
        /// The number of extra cells used for header information like memberId or data length.
        /// </summary>
        public int PrefixCellCount { get; internal set; }

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
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on
        /// the FPGA for transformed algorithms from an existing byte array.
        /// </summary>
        /// <param name="memory">The source data.</param>
        /// <param name="prefixCellCount">The amount of cells for header data. See <see cref="PrefixCellCount"/>.</param>
        /// <param name="alignment">
        /// The alignment value. If set to greater than 0, the starting address of the content is aligned to be a
        /// multiple of that number. It must be an integer and power of 2.
        /// </param>
        /// <remarks>
        /// This constructor is internal only to avoid dependency issues where we have to include the System.Memory
        /// package everywhere where SimpleMemory is used even if it's created with the other constructors. Instead,
        /// you can use
        /// <see cref="SimpleMemoryAccessor.Create(Memory{byte}, int)"/> to construct a <see cref="SimpleMemory"/> from
        /// <see cref="Memory{byte}"/>.
        /// </remarks>
        internal SimpleMemory(Memory<byte> memory, int prefixCellCount, int alignment)
        {
            if (alignment > 0)
            {
                IntPtr address;
                unsafe
                {
                    fixed (byte* pointer = memory.Span)
                    {
                        address = new IntPtr(pointer);
                    }
                }

                var alignmentOffset = alignment - (int)(address.ToInt64() % alignment);
                memory = memory.Slice(alignmentOffset, memory.Length - alignment);
            }

            PrefixedMemory = memory;
            PrefixCellCount = prefixCellCount;
        }

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

        /// <summary>
        /// Creates a new instance of <see cref="SimpleMemory"/> with a specific size of payload in cells using a
        /// device's <see cref="MemoryConfiguration"/>.
        /// </summary>
        /// <param name="memoryConfiguration">Creation parameters associated with the selected device.</param>
        /// <param name="cellCount">The size of the usable memory.</param>
        /// <returns>The instance with a byte[] of capacity for the require payload size.</returns>
        public static SimpleMemory Create(IMemoryConfiguration memoryConfiguration, int cellCount)
        {
            var memory = new byte[((cellCount + memoryConfiguration.MinimumPrefix) * MemoryCellSizeBytes) +
                                  memoryConfiguration.Alignment];
            return new SimpleMemory(memory, memoryConfiguration.MinimumPrefix, memoryConfiguration.Alignment);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SimpleMemory"/> which wraps the given memory as content.
        /// </summary>
        /// <param name="memoryConfiguration">Creation parameters associated with the selected device.</param>
        /// <param name="memory">The data to be assigned.</param>
        /// <param name="logger">Optional logger for reporting issues.</param>
        /// <param name="withPrefixCells">The number of cells already provisioned in the <see cref="memory"/>.</param>
        /// <returns>A new instance that wraps the given memory.</returns>
        /// <remarks>
        /// If the <see cref="IMemoryConfiguration"/> indicates that more prefix space is required than what is already
        /// provisioned in the <see cref="memory"/> according to the <see cref="withPrefixCells"/>, then an additional
        /// copy will occur. This is logged as a warning if a logger is given.
        /// </remarks>
        public static SimpleMemory Create(
            IMemoryConfiguration memoryConfiguration,
            Memory<byte> memory,
            ILogger logger = null,
            int withPrefixCells = 0)
        {
            if (withPrefixCells < memoryConfiguration.MinimumPrefix)
            {
                logger?.LogWarning("Not enough prefix cells available. An extra copy occurs.");
                var additionalBytes = (memoryConfiguration.MinimumPrefix - withPrefixCells) * MemoryCellSizeBytes;
                Memory<byte> newMemory = new byte[memory.Length + additionalBytes];
                memory.CopyTo(newMemory.Slice(additionalBytes));

                memory = newMemory;
                withPrefixCells = memoryConfiguration.MinimumPrefix;
            }

            return new SimpleMemory(memory, withPrefixCells, 0);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SimpleMemory"/> for use in software runs. It handles the content
        /// identically to an instance created for a specific device but has no specific optimizations.
        /// </summary>
        /// <param name="cellCount">The size of the usable memory.</param>
        /// <returns>The instance with a <c>byte[]</c> of capacity for the require payload size.</returns>
        public static SimpleMemory CreateSoftwareMemory(int cellCount) =>
            new(new byte[cellCount * MemoryCellSizeBytes], 0, 0);
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
            if (!MemoryMarshal.TryGetArray(bytes, out var arraySegment))
            {
                throw new NotSupportedException("This Memory does not support exposing the underlying array.");
            }

            return arraySegment;
        }
    }
}
