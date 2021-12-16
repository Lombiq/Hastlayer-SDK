using System;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class Fix64CalculatorSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Fix64Calculator>();
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var fixed64Calculator = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new Fix64Calculator(), configuration);

            var sum = fixed64Calculator.CalculateIntegerSumUpToNumber(10_000_000, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            // This takes about 274ms on an i7 processor with 4 physical (8 logical) cores and 1300ms on an FPGA (with
            // a MaxDegreeOfParallelism of 12 while the device is about half utilized; above that the design will get
            // unstable).
            // Since this basically does what the single-threaded sample but in multiple copies on multiple threads
            // the single-threaded sample takes the same amount of time on the FPGA.

            // Creating an array of numbers alternating between 9999999 and 10000001 so we can also see that threads
            // don't step on each other's feet.
            var numbers = new int[Fix64Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 10_000_000 + (i % 2 == 0 ? -1 : 1);
            }

            fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(
                numbers,
                hastlayer,
                hardwareRepresentation.HardwareGenerationConfiguration);
        }

        public static void RunSoftwareBenchmark()
        {
            var fixed64Calculator = new Fix64Calculator();

            var numbers = new int[Fix64Calculator.MaxDegreeOfParallelism];
            for (int i = 0; i < Fix64Calculator.MaxDegreeOfParallelism; i++)
            {
                numbers[i] = 10_000_000 + (i % 2 == 0 ? -1 : 1);
            }

            var sums = fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            sums = fixed64Calculator.ParallelizedCalculateIntegerSumUpToNumbers(numbers);
            sw.Stop();
            Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds);
        }
    }
}
