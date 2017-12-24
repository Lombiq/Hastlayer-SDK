using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Algorithms;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    public class Fix64Showcase
    {
        public const int Run_InputUInt32Index = 0;
        public const int Run_OutputUInt32Index = 0;


        public virtual void Run(SimpleMemory memory)
        {
            var input = memory.ReadInt32(Run_InputUInt32Index);

            var number = new Fix64(input);
            number *= new Fix64(3);

            var integers = number.ToIntegers();
            memory.WriteInt32(Run_OutputUInt32Index, integers[0]);
            memory.WriteInt32(Run_OutputUInt32Index + 1, integers[1]);
        }
    }


    public static class Fix64ShowcaseeExtensions
    {
        public static Fix64 Run(this Fix64Showcase algorithm, int input)
        {
            var memory = new SimpleMemory(2);

            memory.WriteInt32(Fix64Showcase.Run_InputUInt32Index, input);

            algorithm.Run(memory);

            return Fix64.FromRawInts(new[]
            {
                memory.ReadInt32(Fix64Showcase.Run_OutputUInt32Index),
                memory.ReadInt32(Fix64Showcase.Run_OutputUInt32Index + 1)
            });
        }
    }
}
