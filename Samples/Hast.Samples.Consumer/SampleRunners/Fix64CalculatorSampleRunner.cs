using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class Fix64CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Fix64Calculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var fixed64Showcase = await hastlayer.GenerateProxy(hardwareRepresentation, new Fix64Calculator());

            var sum = fixed64Showcase.CalculateIntegerSumUpToNumber(10000000);

            // This takes about 264ms on an i7 processor with 4 physical (8 logical) cores and 1300ms on an FPGA (with 
            // a MaxDegreeOfParallelism of 10 while the device is about 51% utilized). With a higher degree of 
            // parallelism it won't fit on the Nexys 4 DDR board's FPGA.
            // Since this basically does what the single-threaded sample but in multiple copies on multiple threads
            // the single-threaded sample takes the same amount of time on the FPGA.

            // Creating an array of numbers alternating between 9999999 and 10000001 so we can also see that threads
            // don't step on each other's feet.
            var numbers = new int[Fix64Calculator.MaxDegreeOfParallelism];
            for (int i = 1; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 10000000 + (i % 2 == 0 ? -1 : 1); 
            }
            var sums = fixed64Showcase.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
        }
    }
}
