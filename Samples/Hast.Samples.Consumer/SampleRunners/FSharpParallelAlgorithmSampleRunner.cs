using Hast.Layer;
using Hast.Samples.FSharpSampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class FSharpParallelAlgorithmSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm>();
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var cpuOjutput = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm().Run(234234);
            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm(), configuration);

            var output1 = parallelAlgorithm.Run(234234);
            var output2 = parallelAlgorithm.Run(123);
            var output3 = parallelAlgorithm.Run(9999);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm().Run(234234);
            sw.Stop();
            System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
