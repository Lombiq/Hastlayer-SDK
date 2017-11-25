using System;
using System.Linq;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Transformer.Abstractions.Configuration;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Transformer.Vhdl.Abstractions.Configuration;

namespace Hast.Samples.Kpz
{
    public partial class Kpz
    {
        private bool _verifyOutput;
        private bool _randomSeedEnable;

        public string VhdlOutputFilePath = @"Hast_IP.vhd";
        public delegate void LogItDelegate(string toLog);
        public LogItDelegate LogItFunction; //Should be AsyncLogIt from ChartForm
        public KpzKernelsInterface Kernels;
        public KpzKernelsGInterface KernelsG;
        public PrngTestInterface KernelsP;


        public async Task InitializeHastlayer(bool verifyOutput, bool randomSeedEnable)
        {
            _verifyOutput = verifyOutput;
            _randomSeedEnable = randomSeedEnable;

            LogItFunction("Creating Hastlayer Factory...");
            using (var hastlayer = await Hastlayer.Create())
            {
                hastlayer.ExecutedOnHardware += (sender, e) =>
                {
                    LogItFunction("Hastlayer timer: " +
                        e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds + "ms (net) / " +
                        e.HardwareExecutionInformation.FullExecutionTimeMilliseconds + " ms (total)"
                    );
                };

                var configuration = new HardwareGenerationConfiguration((await hastlayer.GetSupportedDevices()).First().Name);
                configuration.VhdlTransformerConfiguration().VhdlGenerationMode = VhdlGenerationMode.Debug;
                configuration.EnableCaching = false;

                LogItFunction("Generating hardware...");

                if (_kpzTarget.HastlayerGAlgorithm())
                {
                    configuration.AddHardwareEntryPointType<KpzKernelsGInterface>();

                    configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<KpzKernelsGInterface>(
                            k => k.ScheduleIterations(null), 0)
                        {
                            MaxDegreeOfParallelism = KpzKernelsGInterface.ParallelTasks
                        });
                }
                else if (_kpzTarget.HastlayerPlainAlgorithm())
                {
                    configuration.AddHardwareEntryPointType<KpzKernelsInterface>();
                }
                else // if (kpzTarget == KpzTarget.PrngTest)
                {
                    configuration.AddHardwareEntryPointType<PrngTestInterface>();
                }


                var hardwareRepresentation = await hastlayer.GenerateHardware(new[] {
                    typeof(KpzKernelsGInterface).Assembly,
                    typeof(Algorithms.PrngMWC64X).Assembly
                }, configuration);

                await hardwareRepresentation.HardwareDescription.WriteSource(VhdlOutputFilePath);

                LogItFunction("Generating proxy...");

                if (_kpzTarget.HastlayerOnFpga())
                {
                    var proxyConf = new ProxyGenerationConfiguration
                    {
                        VerifyHardwareResults = _verifyOutput
                    };

                    if (_kpzTarget == KpzTarget.Fpga)
                    {
                        Kernels = await hastlayer.GenerateProxy(
                            hardwareRepresentation,
                            new KpzKernelsInterface(),
                            proxyConf);
                    }
                    else if (_kpzTarget == KpzTarget.FpgaG)
                    {
                        KernelsG = await hastlayer.GenerateProxy(
                            hardwareRepresentation,
                            new KpzKernelsGInterface(),
                            proxyConf);
                    }
                    else //if(kpzTarget == KpzTarget.PrngTest) 
                    {
                        KernelsP = await hastlayer.GenerateProxy(
                            hardwareRepresentation,
                            new PrngTestInterface(),
                            proxyConf);
                    }

                    LogItFunction("FPGA target detected");
                }
                else //if (kpzTarget == KpzTarget.HastlayerSimulation())
                {
                    Kernels = new KpzKernelsInterface();
                    KernelsG = new KpzKernelsGInterface();
                    LogItFunction("Simulation target detected");
                }

                if (_kpzTarget.HastlayerPlainAlgorithm())
                {
                    LogItFunction("Running TestAdd...");
                    uint resultFpga = Kernels.TestAddWrapper(4313, 123);
                    uint resultCpu = 4313 + 123;
                    if (resultCpu == resultFpga) LogItFunction(string.Format("Success: {0} == {1}", resultFpga, resultCpu));
                    else LogItFunction(string.Format("Fail: {0} != {1}", resultFpga, resultCpu));
                }

                if (_kpzTarget == KpzTarget.PrngTest)
                {
                    LogItFunction("Running TestPrng...");

                    var kernelsCpu = new PrngTestInterface();
                    ulong randomSeed = 0x37a92d76a96ef210UL;
                    var smCpu = kernelsCpu.PushRandomSeed(randomSeed);
                    var smFpga = KernelsP.PushRandomSeed(randomSeed);
                    LogItFunction("PRNG results:");
                    bool success = true;

                    for (int PrngTestIndex = 0; PrngTestIndex < 10; PrngTestIndex++)
                    {
                        uint prngCpuResult = kernelsCpu.GetNextRandom(smCpu);
                        uint prngFpgaResult = KernelsP.GetNextRandom(smFpga);
                        if (prngCpuResult != prngFpgaResult) { success = false; }
                        LogItFunction(String.Format("{0}, {1}", prngCpuResult, prngFpgaResult));
                    }

                    if (success) LogItFunction("TestPrng succeeded!");
                    else LogItFunction("TestPrng failed!");
                }
            }
        }
    }
}
