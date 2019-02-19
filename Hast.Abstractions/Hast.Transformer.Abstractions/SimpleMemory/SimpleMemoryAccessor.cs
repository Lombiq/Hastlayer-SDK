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
        /// <param name="prefixCells">The length of the prefix in cells. It must not be greater than <see cref="SimpleMemory.PrefixCellCount"/>.</param>
        /// <returns></returns>
        public Memory<byte> Get(int prefixCells)
        {
            if (prefixCells > _simpleMemory.PrefixCellCount || prefixCells < 0)
                throw new ArgumentOutOfRangeException($"You can use 0-{_simpleMemory.PrefixCellCount} cells for prefix!");

            return _simpleMemory.PrefixedMemory.Slice((_simpleMemory.PrefixCellCount - prefixCells) * SimpleMemory.MemoryCellSizeBytes);
        }

        /// <summary>
        /// Sets the internal value of the SimpleMemory with the first prefixCells amount of cells hidden.
        /// </summary>
        /// <param name="data">The new data.</param>
        /// <param name="prefixCells">The amount of cells to be shifted out.</param>
        /// <remarks>
        /// Using prefixCells allows you to set the communication headers during Get without an extra copy,  but you must
        /// use at least as many prefixCells for Set as for Get in continuous usage, otherwise ArgumentOutOfRangeException
        /// will be thrown eventually.
        /// </remarks>
        public void Set(Memory<byte> data, int prefixCells = 0)
        {
            _simpleMemory.PrefixedMemory = data;
            if (prefixCells < _simpleMemory.PrefixCellCount) _simpleMemory.PrefixCellCount = prefixCells;
        }

        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on 
        /// the FPGA for transformed algorithms from an existing byte array. Keep in mind that each 32b segment in the
        /// given <see cref="Memory{T}"/> object will correspond to a cell in <see cref="SimpleMemory"/>, indexed from
        /// 0.
        /// </summary>
        /// <param name="data">The data to be put into the <see cref="SimpleMemory"/>.</param>
        /// <param name="prefixCells">The amount of cells used for prefix/header.</param>
        /// <returns>The <see cref="SimpleMemory"/> containing the data.</returns>
        /// <remarks>If data is a byte[] you can use the <see cref="SimpleMemory"/> constructor instead.</remarks>
        public static SimpleMemory Create(Memory<byte> data, int prefixCells = 0) => new SimpleMemory(data, prefixCells);

        /// <summary>
        /// Creates a new <see cref="SimpleMemory"/> instance from the specified data and its
        /// <see cref="SimpleMemoryAccessor"/> at the same time.
        /// </summary>
        /// <param name="data">The data to be put into the <see cref="SimpleMemory"/>.</param>
        /// <param name="prefixCells">The amount of cells used for prefix/header.</param>
        /// <param name="accessor">The accessor of the return value.</param>
        /// <returns>The <see cref="SimpleMemory"/> containing the data.</returns>
        public static SimpleMemory Create(Memory<byte> data, int prefixCells, out SimpleMemoryAccessor accessor)
        {
            var memory = new SimpleMemory(data, prefixCells);
            accessor = new SimpleMemoryAccessor(memory);
            return memory;
        }
    }
}
