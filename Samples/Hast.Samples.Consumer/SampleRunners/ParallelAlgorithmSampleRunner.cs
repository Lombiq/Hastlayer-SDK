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

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm(), configuration);

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var output1 = parallelAlgorithm.Run(234234, memoryConfig);
            var output2 = parallelAlgorithm.Run(123, memoryConfig);
            var output3 = parallelAlgorithm.Run(9999, memoryConfig);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ParallelAlgorithm().Run(234234, memoryConfig);
            sw.Stop();
            System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
