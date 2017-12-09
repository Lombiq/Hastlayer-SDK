using System;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;
using Hast.Transformer.Vhdl.Abstractions.Configuration;

namespace Hast.Samples.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                using (var hastlayer = await Hastlayer.Create())
                {
                    #region Configuration
                    var configuration = new HardwareGenerationConfiguration("Nexys4 DDR");

                    configuration.AddHardwareEntryPointType<ParallelAlgorithm>();

                    configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
                        {
                            MaxDegreeOfParallelism = ParallelAlgorithm.MaxDegreeOfParallelism
                        });

                    configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

                    hastlayer.ExecutedOnHardware += (sender, e) =>
                    {
                        Console.WriteLine(
                            "Executing " +
                            e.MemberFullName +
                            " on hardware took " +
                            e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                            "ms (net) " +
                            e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                            " milliseconds (all together)");
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

                    await hardwareRepresentation.HardwareDescription.WriteSource("Hast_IP.vhd");
                    #endregion

                    #region Execution
                    Console.WriteLine("Starting hardware execution.");
                    var parallelAlgorithm = await hastlayer.GenerateProxy(hardwareRepresentation, new ParallelAlgorithm());

                    var output1 = parallelAlgorithm.Run(234234);
                    var output2 = parallelAlgorithm.Run(123);
                    var output3 = parallelAlgorithm.Run(9999);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var cpuOutput = new ParallelAlgorithm().Run(234234);
                    sw.Stop();
                    Console.WriteLine("On CPU it took " + sw.ElapsedMilliseconds + "ms.");
                    #endregion
                }
            }).Wait();

            Console.ReadKey();
        }
    }
}
