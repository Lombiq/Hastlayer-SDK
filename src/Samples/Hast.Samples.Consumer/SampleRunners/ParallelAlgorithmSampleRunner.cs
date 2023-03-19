using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal class ParallelAlgorithmSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

    // Note that Hastlayer can figure out how many Tasks will be there to an extent (see comment in
    // ParallelAlgorithm) but if it can't, use a configuration like below:
    //// configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
    ////     new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
    ////     {
    ////         MaxDegreeOfParallelism = ParallelAlgorithm.MaxDegreeOfParallelism
    ////     });

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        long RunLogAndTime(ParallelAlgorithm parallelAlgorithm, int input)
        {
            var stopwatch = Stopwatch.StartNew();

            var output = parallelAlgorithm.Run(input, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            Console.WriteLine(
                "{0}.{1}({2}) == {3}",
                nameof(ParallelAlgorithm),
                nameof(ParallelAlgorithm.Run),
                input,
                output);

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        var numbers = new[] { 234234, 123, 9999 };

        // Execute with FPGA.
        var parallel = await hastlayer.GenerateProxyAsync(
            hardwareRepresentation,
            new ParallelAlgorithm(),
            configuration);
        foreach (var number in numbers) RunLogAndTime(parallel, number);

        // Execute with CPU.
        parallel = new ParallelAlgorithm(); // Replace proxy with CPU implementation.
        foreach (var number in numbers)
        {
            Console.WriteLine(FormattableString.Invariant($"On CPU it took {RunLogAndTime(parallel, number)}ms."));
        }
    }
}
