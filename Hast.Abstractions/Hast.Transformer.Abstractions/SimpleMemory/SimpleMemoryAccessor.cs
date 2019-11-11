using System;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    /// <summary>
    /// Facilitates read and overwrite of the <see cref="SimpleMemory.Memory"/> for outside users. (like DMA)
    /// </summary>
    public class SimpleMemoryAccessor
    {
        private SimpleMemory _simpleMemory;


        public SimpleMemoryAccessor(SimpleMemory simpleMemory)
        {
            _simpleMemory = simpleMemory;
        }


        public Memory<byte> Get() => _simpleMemory.Memory;

        /// <summary>
        /// Gets the memory contents with additional prefix.
        /// </summary>
        /// <param name="prefixCellCount">
        /// The length of the prefix in cells. It must not be greater than <see cref="SimpleMemory.PrefixCellCount"/>.
        /// </param>
        /// <returns></returns>
        public Memory<byte> Get(int prefixCellCount)
        {
            if (prefixCellCount < 0)
                throw new ArgumentOutOfRangeException($"{nameof(prefixCellCount)} must be positive!");

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
        /// Sets the internal value of the SimpleMemory with the first prefixCellCount amount of cells hidden.
        /// </summary>
        /// <param name="data">The new data.</param>
        /// <param name="prefixCellCount">The amount of cells to be shifted out.</param>
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
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on 
        /// the FPGA for transformed algorithms from an existing byte array. Keep in mind that each 32b segment in the
        /// given <see cref="Memory{T}"/> object will correspond to a cell in <see cref="SimpleMemory"/>, indexed from
        /// 0.
        /// </summary>
        /// <param name="data">The data to be put into the <see cref="SimpleMemory"/>.</param>
        /// <returns>The <see cref="SimpleMemory"/> containing the data.</returns>
        /// <remarks>If data is a byte[] you can use the <see cref="SimpleMemory"/> constructor instead.</remarks>
        public static SimpleMemory Create(Memory<byte> data) => new SimpleMemory(data, 0);

        /// <summary>
        /// Creates a new <see cref="SimpleMemory"/> instance from the specified data and its
        /// <see cref="SimpleMemoryAccessor"/> at the same time.
        /// </summary>
        /// <param name="data">The data to be put into the <see cref="SimpleMemory"/>.</param>
        /// <param name="accessor">The accessor of the return value.</param>
        /// <returns>The <see cref="SimpleMemory"/> containing the data.</returns>
        public static SimpleMemory Create(Memory<byte> data, out SimpleMemoryAccessor accessor)
        {
            var memory = new SimpleMemory(data, 0);
            accessor = new SimpleMemoryAccessor(memory);
            return memory;
        }
    }
}
