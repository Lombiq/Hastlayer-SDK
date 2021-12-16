using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System;
using System.Threading.Tasks;

namespace Hast.Samples.Demo
{
    internal static class Program
    {
        private static async Task Main()
        {
            using var hastlayer = Hastlayer.Create();

            #region Configuration
            var configuration = new HardwareGenerationConfiguration("Nexys A7", "HardwareFramework");

            configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

            configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

            hastlayer.ExecutedOnHardware += (sender, e) =>
            {
                Console.WriteLine(
                    "Executing on hardware took " +
                    e.Arguments.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                    " milliseconds (net) " +
                    e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                    " milliseconds (all together).");
            };
            #endregion

            #region HardwareGeneration
            Console.WriteLine("Hardware generation starts.");
            var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
                new[]
                {
                    typeof(ParallelAlgorithm).Assembly,
                },
                configuration);
            #endregion

            #region Execution
            Console.WriteLine("Hardware generated, starting software execution.");
            Console.WriteLine();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ParallelAlgorithm().Run(234234, null);
            sw.Stop();

            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " milliseconds.");

            Console.WriteLine();
            Console.WriteLine("Starting hardware execution.");

            var parallelAlgorithm = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new ParallelAlgorithm());

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var output1 = parallelAlgorithm.Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output2 = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output3 = parallelAlgorithm.Run(9999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            #endregion
        }
    }
}
