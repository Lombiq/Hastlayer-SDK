using Hast.Layer;
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
        private const int RunInputOutputInt32Index = 0;

        public virtual void Run(SimpleMemory memory) =>
            // Adding 1 to the input so it's visible whether this actually has run, not just the untouched data was
            // sent back.
            memory.WriteInt32(RunInputOutputInt32Index, memory.ReadInt32(RunInputOutputInt32Index) + 1);

        public int Run(
            int input,
            IHastlayer hastlayer = null,
            IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(1)
                : hastlayer.CreateMemory(configuration, 1);
            memory.WriteInt32(RunInputOutputInt32Index, input);
            Run(memory);
            return memory.ReadInt32(RunInputOutputInt32Index);
        }
    }
}
