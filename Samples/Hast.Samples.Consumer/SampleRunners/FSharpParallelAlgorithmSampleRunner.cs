using Hast.Layer;
using Hast.Samples.FSharpSampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class FSharpParallelAlgorithmSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) => configuration.AddHardwareEntryPointType<FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm>();

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            _ = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm().Run(234_234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm(), configuration).ConfigureAwait(true);

            _ = parallelAlgorithm.Run(234_234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(9_999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            _ = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm().Run(234_234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            sw.Stop();
            System.Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
        }
    }
}
