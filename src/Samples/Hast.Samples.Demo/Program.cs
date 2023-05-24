using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Configuration;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;

using var hastlayer = Hastlayer.Create();

#region Configuration

var configuration = new HardwareGenerationConfiguration("Nexys A7");

configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

hastlayer.ExecutedOnHardware += (_, e) =>
    Console.WriteLine(
        StringHelper.ConcatenateConvertiblesInvariant(
            "Executing on hardware took ",
            e.Arguments.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds,
            " milliseconds (net) ",
            e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds,
            " milliseconds (all together)."));

#endregion Configuration

#region HardwareGeneration

Console.WriteLine("Hardware generation starts.");
var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
    new[]
    {
        typeof(ParallelAlgorithm).Assembly,
    },
    configuration);

#endregion HardwareGeneration

#region Execution

Console.WriteLine("Hardware generated, starting software execution.");
Console.WriteLine();

var sw = System.Diagnostics.Stopwatch.StartNew();
var cpuOutput = new ParallelAlgorithm().Run(234234);
sw.Stop();

Console.WriteLine(StringHelper.ConcatenateConvertiblesInvariant("On CPU it took ", sw.ElapsedMilliseconds, " milliseconds."));

Console.WriteLine();
Console.WriteLine("Starting hardware execution.");

var parallelAlgorithm = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new ParallelAlgorithm());

var memoryConfig = hastlayer.CreateMemoryConfiguration(hardwareRepresentation);
var output1 = parallelAlgorithm.Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
var output2 = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
var output3 = parallelAlgorithm.Run(9999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
#endregion Execution
