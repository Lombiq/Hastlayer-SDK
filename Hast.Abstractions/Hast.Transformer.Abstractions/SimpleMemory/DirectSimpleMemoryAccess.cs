using System;

namespace Hast.Transformer.Abstractions.SimpleMemory
{
    /// <summary>
    /// Facilitates read and overwrite of the <see cref="SimpleMemory.Memory"/> for outside users. (like DMA)
    /// </summary>
    public class DirectSimpleMemoryAccess
    {
        private SimpleMemory _simpleMemory;

        public DirectSimpleMemoryAccess(SimpleMemory simpleMemory)
        {
            _simpleMemory = simpleMemory;
        }

        public Memory<byte> Get() => _simpleMemory.Memory;

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
        /// <param name="prefixCells">The amound of cells to be shifted out.</param>
        /// <remarks>
        /// Using prefixCells allows you to set the communication headers during Get without an extra copy,  but you must
        /// use at least as many prefixCells for Set as for Get in continuous usage, otherwise ArgumentOutOfRangeException
        /// will be thrown eventually.
        /// </remarks>
        public void Set(Memory<byte> data, int prefixCells = 0)
        {
            if (prefixCells > _simpleMemory.PrefixCellCount || prefixCells < 0)
                throw new ArgumentOutOfRangeException($"You can use 0-{_simpleMemory.PrefixCellCount} cells for prefix!");

            _simpleMemory.PrefixedMemory = data;
            if (prefixCells < _simpleMemory.PrefixCellCount) _simpleMemory.PrefixCellCount = prefixCells;
        }
    }
}
