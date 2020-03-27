using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System;
using System.Threading.Tasks;

namespace Hast.Samples.Demo
{
    class Program
    {
        private static void Main()
        {
            Task.Run(async () =>
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
                var hardwareRepresentation = await hastlayer.GenerateHardware(
                    new[]
                    {
                        typeof(ParallelAlgorithm).Assembly
                    },
                    configuration);
                #endregion

                #region Execution
                Console.WriteLine("Hardware generated, starting software execution.");
                Console.WriteLine();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cpuOutput = new ParallelAlgorithm().Run(234234);
                sw.Stop();

                Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " milliseconds.");

                Console.WriteLine();
                Console.WriteLine("Starting hardware execution.");

                var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

                var output1 = parallelAlgorithm.Run(234234);
                var output2 = parallelAlgorithm.Run(123);
                var output3 = parallelAlgorithm.Run(9999);
                #endregion
            }).Wait();

            Console.ReadKey();
        }
    }
}
