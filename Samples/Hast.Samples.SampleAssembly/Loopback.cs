using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample that simply sends back the input. This can be used to test connectivity to the FPGA as well as to see
    /// the baseline resource usage of the Hastlayer Hardware Framework.
    /// </summary>
    public class Loopback
    {
        public const int Run_InputOutputUInt32Index = 0;


        public virtual void Run(SimpleMemory memory)
        {
            // Adding 1 to the input so it's visible whether this actually has run, not just the untouched data was
            // sent back.
            memory.WriteUInt32(Run_InputOutputUInt32Index, memory.ReadUInt32(Run_InputOutputUInt32Index) + 1);
        }
    }


    public static class LoopbackExtensions
    {
        public static uint Run(this Loopback loopback, uint input)
        {
            var memory = new SimpleMemory(1);
            memory.WriteUInt32(Loopback.Run_InputOutputUInt32Index, input);
            loopback.Run(memory);
            return memory.ReadUInt32(Loopback.Run_InputOutputUInt32Index);
        }
    }
}
