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
            RunSoftwareBenchmark();
            configuration.AddHardwareEntryPointType<Fix64Calculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var fixed64Calculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Fix64Calculator());

            var sum = fixed64Calculator.CalculateIntegerSumUpToNumber(10000000);

            // This takes about 252ms on an i7 processor with 4 physical (8 logical) cores and 1300ms on an FPGA (with 
            // a MaxDegreeOfParallelism of 10 while the device is about 58% utilized). With a higher degree of 
            // parallelism it won't fit on the Nexys 4 DDR board's FPGA.
            // Since this basically does what the single-threaded sample but in multiple copies on multiple threads
            // the single-threaded sample takes the same amount of time on the FPGA.

            // Creating an array of numbers alternating between 9999999 and 10000001 so we can also see that threads
            // don't step on each other's feet.
            var numbers = new int[Fix64Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 10000000 + (i % 2 == 0 ? -1 : 1); 
            }
            var sums = fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
        }

        public static void RunSoftwareBenchmark()
        {
            var fixed64Calculator = new Fix64Calculator();

            var numbers = new int[Fix64Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 10000000 + (i % 2 == 0 ? -1 : 1);
            }
            var sums = fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            sums = fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
            sw.Stop();
            Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds);
        }
    }
}
