using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System;
using System.Threading.Tasks;

namespace Hast.Samples.Demo
{
    internal class Program
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
                    e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                    " milliseconds (net) " +
                    e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
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
            _ = new ParallelAlgorithm().Run(234_234);
            sw.Stop();

            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " milliseconds.");

            Console.WriteLine();
            Console.WriteLine("Starting hardware execution.");

            var parallelAlgorithm = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new ParallelAlgorithm());

            _ = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            _ = parallelAlgorithm.Run(234_234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            _ = parallelAlgorithm.Run(9_999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            #endregion
        }
    }
}
