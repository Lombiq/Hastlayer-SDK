using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample that simply sends back the input plus one. This can be used to test connectivity to the FPGA as well
    /// as to see the baseline resource usage of the Hastlayer Hardware Framework. It can also serve as a generic
    /// testbed that you can quickly modify to try out small pieces of code.
    /// </summary>
    public class Loopback
    {
        private const int Run_InputOutputInt32Index = 0;


        public virtual void Run(SimpleMemory memory)
        {
            // Adding 1 to the input so it's visible whether this actually has run, not just the untouched data was
            // sent back.
            memory.WriteInt32(Run_InputOutputInt32Index, memory.ReadInt32(Run_InputOutputInt32Index) + 1);
        }

        public int Run(int input, IMemoryConfiguration memoryConfiguration)
        {
            var memory = SimpleMemory.Create(memoryConfiguration, 1);
            memory.WriteInt32(Run_InputOutputInt32Index, input);
            Run(memory);
            return memory.ReadInt32(Run_InputOutputInt32Index);
        }
    }
}
