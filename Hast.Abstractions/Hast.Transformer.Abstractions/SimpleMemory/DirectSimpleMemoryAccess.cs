using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Memory<byte> Get() => _simpleMemory.Memory.ToArray();
        public void Set(Memory<byte> data) => _simpleMemory.Memory = data;
    }
}
