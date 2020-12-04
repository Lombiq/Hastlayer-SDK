using System;
using System.IO;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    /// <summary>
    /// Facilitates read and overwrite of the <see cref="SimpleMemory.Memory"/> for outside users (like DMA).
    /// </summary>
    public class SimpleMemoryAccessor
    {
        private readonly SimpleMemory _simpleMemory;

        public SimpleMemoryAccessor(SimpleMemory simpleMemory) => _simpleMemory = simpleMemory;

        /// <summary>
        /// Gets the memory contents without an additional prefix.
        /// </summary>
        /// <remarks>
        /// Here besides <see cref="Get(int)"/> so for simpler cases there's no branching necessary.
        /// </remarks>
        public Memory<byte> Get() => _simpleMemory.Memory;

        /// <summary>
        /// Gets the memory contents with additional prefix.
        /// </summary>
        /// <param name="prefixCellCount">
        /// The length of the prefix in cells. It must not be greater than <see cref="SimpleMemory.PrefixCellCount"/>.
        /// On what this means see the remarks on <see cref="Set(Memory{byte}, int)"/>.
        /// </param>
        public Memory<byte> Get(int prefixCellCount)
        {
            if (prefixCellCount < 0)
                throw new ArgumentOutOfRangeException($"{nameof(prefixCellCount)} must be positive!");

            if (prefixCellCount == 0) return Get();

            // If we need more prefix than what is available.
            if (prefixCellCount > _simpleMemory.PrefixCellCount)
            {
                int missingBytes = prefixCellCount - _simpleMemory.PrefixCellCount * SimpleMemory.MemoryCellSizeBytes;
                Memory<byte> newMemory = new byte[_simpleMemory.PrefixedMemory.Length + missingBytes];
                _simpleMemory.PrefixedMemory.CopyTo(newMemory.Slice(missingBytes));

                _simpleMemory.PrefixCellCount = prefixCellCount;
                return _simpleMemory.PrefixedMemory = newMemory;
            }

            return _simpleMemory.PrefixedMemory.Slice((_simpleMemory.PrefixCellCount - prefixCellCount) * SimpleMemory.MemoryCellSizeBytes);
        }

        /// <summary>
        /// Sets the internal value of the SimpleMemory with the first prefixCellCount cells hidden.
        /// </summary>
        /// <param name="data">The new data.</param>
        /// <param name="prefixCellCount">
        /// The number of cells to be used as the <see cref="SimpleMemory.PrefixCellCount"/> value for the underlying
        /// <see cref="SimpleMemory"/>.
        /// </param>
        /// <remarks>
        /// Using prefixCellCount allows you to set the communication headers during Get without an extra copy, but you
        /// must use at least as many prefixCellCount for Set as for Get if the <see cref="SimpleMemory"/> is reused,
        /// otherwise the memory has to be copied and that incurs a performance hit.
        /// </remarks>
        public void Set(Memory<byte> data, int prefixCellCount = 0)
        {
            _simpleMemory.PrefixedMemory = data;
            _simpleMemory.PrefixCellCount = prefixCellCount;
        }

        /// <summary>
        /// Saves the underlying <see cref="SimpleMemory"/> to a binary storage format.
        /// </summary>
        /// <param name="stream">The stream to write the storage data to.</param>
        public void Store(Stream stream)
        {
            var segment = Get().GetUnderlyingArray();
            stream.Write(segment.Array, segment.Offset, _simpleMemory.ByteCount);
        }

        /// <summary>
        /// Saves the underlying <see cref="SimpleMemory"/> to a binary storage format to a file. Overwrites the file
        /// if it exists, creates it otherwise.
        /// </summary>
        /// <param name="filePath">The path under to write the storage data file to.</param>
        public void Store(string filePath)
        {
            using (var fileStream = File.Create(filePath))
                Store(fileStream);
        }

        /// <summary>
        /// Loads a binary storage format into the underlying <see cref="SimpleMemory"/> with the first prefixCellCount
        /// cells hidden.
        /// </summary>
        /// <param name="stream">The stream to read the storage data from.</param>
        /// <param name="prefixCellCount">
        /// The number of cells to be used as the <see cref="SimpleMemory.PrefixCellCount"/> value for the underlying
        /// <see cref="SimpleMemory"/>.
        /// </param>
        public void Load(Stream stream, int prefixCellCount = 0)
        {
            int prefixBytesCount = prefixCellCount * SimpleMemory.MemoryCellSizeBytes;
            var data = new byte[stream.Length + prefixBytesCount];
            stream.Read(data, prefixBytesCount, (int)stream.Length);
            Set(data, prefixCellCount);
        }

        /// <summary>
        /// Loads a binary storage format into the underlying <see cref="SimpleMemory"/> with the first prefixCellCount
        /// cells hidden.
        /// </summary>
        /// <param name="filePath">The path of the file to read the storage data from.</param>
        /// <param name="prefixCellCount">
        /// The number of cells to be used as the <see cref="SimpleMemory.PrefixCellCount"/> value for the underlying
        /// <see cref="SimpleMemory"/>.
        /// </param>
        public void Load(string filePath, int prefixCellCount = 0)
        {
            using (var fileStream = File.OpenRead(filePath))
                Load(fileStream, prefixCellCount);
        }
    }
}
