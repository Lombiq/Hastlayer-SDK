using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class ParallelAlgorithmSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

            configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
                {
                    MaxDegreeOfParallelism = ParallelAlgorithm.MaxDegreeOfParallelism
                });
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

            // This takes about 1900ms on an i7 processor with 4 physical (8 logical) cores and 300ms on an FPGA (with 
            // a MaxDegreeOfParallelism of 280 while the device is about 80% utilized). With a higher degree of 
            // parallelism it won't fit on the Nexys 4 DDR board's FPGA.
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
