using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Hast.Samples.Demo
{
    [SuppressMessage(
        "Globalization",
        "CA1303:Do not pass literals as localized parameters",
        Justification = "We are trying to keep this sample to-the-point so there is no localization.")]
    public static class Program
    {
        private static async Task Main()
        {
            using var hastlayer = Hastlayer.Create();

            var configuration = Configure(
                hastlayer,
                "Nexys A7",
                "HardwareFramework");

            // HardwareGeneration
            Console.WriteLine("Hardware generation starts.");
            var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
                new[]
                {
                    typeof(ParallelAlgorithm).Assembly,
                },
                configuration);

            await ExecuteAsync(hastlayer, hardwareRepresentation);
        }

        private static HardwareGenerationConfiguration Configure(IHastlayer hastlayer, string deviceName, string hardwareFrameworkPath)
        {
            var configuration = new HardwareGenerationConfiguration(deviceName, hardwareFrameworkPath);

            configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

            configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

            hastlayer.ExecutedOnHardware += (_, e) =>
                Console.WriteLine(
                    "Executing on hardware took " +
                    e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                    " milliseconds (net) " +
                    e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                    " milliseconds (all together).");

            return configuration;
        }

        private static async Task ExecuteAsync(
            IHastlayer hastlayer,
            IHardwareRepresentation hardwareRepresentation)
        {
            Console.WriteLine("Hardware generated, starting software execution.");
            Console.WriteLine();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            _ = new ParallelAlgorithm().Run(234_234);
            sw.Stop();

            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " milliseconds.");

            Console.WriteLine();
            Console.WriteLine("Starting hardware execution.");

            var parallelAlgorithm = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new ParallelAlgorithm());

#pragma warning disable S3215 // "interface" instances should not be cast to concrete types
            _ = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
#pragma warning restore S3215 // "interface" instances should not be cast to concrete types

            _ = parallelAlgorithm.Run(234_234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(9_999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
