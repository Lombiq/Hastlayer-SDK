using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using Hast.Xilinx.Abstractions.ManifestProviders;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Samples.Demo
{
    internal class Program
    {
        private static async Task Main()
        {
            using var hastlayer = Hastlayer.Create();

            #region Configuration
            var configuration = new HardwareGenerationConfiguration(TrenzTE071504301CManifestProvider.DeviceName, "HardwareFramework");
            configuration.SingleBinaryPath = "/media/sd-mmcblk0p1/demo/parallel_algorithm.xclbin";
            var isDevice = File.Exists(configuration.SingleBinaryPath);

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
                    typeof(ParallelAlgorithm).Assembly,
                },
                configuration);
            #endregion

            if (!isDevice)
            {
                Console.WriteLine("This is a build machine. No execution will be performed.");
                return;
            }

            #region Execution
            Console.WriteLine("Hardware generated, starting software execution.");
            Console.WriteLine();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cpuOutput = new ParallelAlgorithm().Run(234234, null);
            sw.Stop();

            Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + " milliseconds.");

            Console.WriteLine();
            Console.WriteLine("Starting hardware execution.");

            var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var output1 = parallelAlgorithm.Run(234234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output2 = parallelAlgorithm.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output3 = parallelAlgorithm.Run(9999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            var stopwatch = Stopwatch.StartNew();
            new ParallelAlgorithm().Run(9999, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            stopwatch.Stop();
            Console.WriteLine("On CPU it took {0}ms.", stopwatch.ElapsedMilliseconds);
            #endregion
        }
    }
}
