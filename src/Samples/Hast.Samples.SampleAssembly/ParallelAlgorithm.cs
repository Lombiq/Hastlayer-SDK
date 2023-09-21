using Hast.Layer;
using Hast.Synthesis.Attributes;
using Hast.Transformer.SimpleMemory;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

public class ParallelAlgorithm
{
    public const int ArrayLength = 10;

    private const int RunInputInt32Index = 0;
    private const int RunOutputInt32Index = 0;

    [Replaceable(nameof(ParallelAlgorithm) + "." + nameof(MaxDegreeOfParallelism))]
    private static readonly int MaxDegreeOfParallelism = 2;

    public virtual void Run(SimpleMemory memory)
    {
        var someInput = memory.ReadInt32(RunInputInt32Index);

        var commonArray = new int[ArrayLength];

        commonArray[0] = someInput;

        var tasks = new Task<TaskOutput>[MaxDegreeOfParallelism];

        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            tasks[i] = Task.Factory.StartNew(
                arrayObject =>
                {
                    var localArray = (int[])arrayObject;

                    // You can read or write the local array here, but you can't access the common array.

                    localArray[0] = 123;

                    // Since Hastlayer doesn't yet support multi-dimensional arrays (see
                    // https://github.com/Lombiq/Hastlayer-SDK/issues/10) you'll need to use some other type for output.

                    return new TaskOutput
                    {
                        OutputArray = localArray,
                        SomeOtherOutput = 42,
                    };
                },
                commonArray);
        }

        Task.WhenAll(tasks).Wait();

        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            // Do something with tasks[i].Result here.
        }

        // Use memory.Write*() to write the output to the memory. You'll be able to read it from the host PC.
        // You can also use the memory (which is actual RAM on the FPGA board) to store temporary data but usually
        // that's not necessary and you can just use local variables. Keep in mind that on the FPGA there's no memory
        // allocation for objects, a variable and any object you put into it will be just a bunch of hardware wires and
        // registers.
    }

    public int Run(int input, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
    {
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);
        memory.WriteInt32(RunInputInt32Index, input);
        Run(memory);
        return memory.ReadInt32(RunOutputInt32Index);
    }

    private sealed class TaskOutput
    {
        public int[] OutputArray { get; set; }
        public int SomeOtherOutput { get; set; }
    }
}
