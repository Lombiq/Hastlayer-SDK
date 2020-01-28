using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ParallelAlgorithmSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

            // Note that Hastlayer can figure out how many Tasks will be there to an extent (see comment in 
            // ParallelAlgorithm) but if it can't, use a configuration like below:
            //configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
            //    new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
            //    {
            //        MaxDegreeOfParallelism = ParallelAlgorithm.MaxDegreeOfParallelism
            //    });
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

            // This takes about 1900 ms on an i7 processor with 4 physical (8 logical) cores and 300 ms on an FPGA
            // (with a MaxDegreeOfParallelism of 280 while the device is about 80% utilized). With a higher degree of
            // parallelism it won't fit on the Nexys A7 board's FPGA.
            // On Catapult a MaxDegreeOfParallelism of 650 will fit as well (80% resource utilization) and runs in
            // about 200 ms (including communication latency) vs about 5s on the previous reference PC. Compiling that
            // hardware design will take about 15 hours though (with MaxDegreeOfParallelism of 600 it'll take about 4).
            var output1 = parallelAlgorithm.Run(234234);
            var output2 = parallelAlgorithm.Run(123);
            var output3 = parallelAlgorithm.Run(9999);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ParallelAlgorithm().Run(234234);
            sw.Stop();
            System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
