using Hast.Layer;
using Hast.Samples.FSharpSampleAssembly;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal sealed class FSharpParallelAlgorithmSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm>();

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        var parallelAlgorithm = await hastlayer.GenerateProxyAsync(
            hardwareRepresentation,
            new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm(),
            configuration);

        // Three sample outputs.
        _ = parallelAlgorithm.Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        _ = parallelAlgorithm.Run(9999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

        // Warming up CPU execution (not to have wrong measurements due to JIT compilation).
        _ = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm()
            .Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

        // CPU execution as a benchmark.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _ = new FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm()
            .Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        sw.Stop();
        System.Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("On CPU it took ", sw.ElapsedMilliseconds, "ms."));
    }
}
