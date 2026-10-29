using Hast.Layer;
using Hast.Transformer.SimpleMemory;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// A sample that simply sends back the input plus one. This can be used to test connectivity to the FPGA as well as to
/// see the baseline resource usage of the Hastlayer Hardware Framework. It can also serve as a generic testbed that you
/// can quickly modify to try out small pieces of code.
/// </summary>
public class Loopback
{
    private const int RunInputOutputInt32Index = 0;

    // We add 1 to the input to verify that it actually runs instead of sending back the input as-is.
    public virtual void Run(SimpleMemory memory) =>
        memory.WriteInt32(RunInputOutputInt32Index, memory.ReadInt32(RunInputOutputInt32Index) + 1);

    public int Run(int input, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);
        memory.WriteInt32(RunInputOutputInt32Index, input);
        Run(memory);
        return memory.ReadInt32(RunInputOutputInt32Index);
    }
}
