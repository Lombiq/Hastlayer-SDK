using Hast.Algorithms.Random;
using Hast.Layer;
using Hast.Samples.Kpz.Algorithms;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Kpz
{
    public partial class Kpz
    {
        private bool _verifyOutput;
        private bool _randomSeedEnable;

        public delegate void LogItDelegate(string toLog);
        public LogItDelegate LogItFunction { get; set; } // Should be AsyncLogIt from ChartForm
        public KpzKernelsInterface Kernels { get; set; }
        public KpzKernelsParallelizedInterface KernelsParallelized { get; set; }
        public PrngTestInterface KernelsP { get; set; }

        public async Task<(IHastlayer Hastlayer, IHardwareGenerationConfiguration Configuration)> InitializeHastlayerAsync(
            bool verifyOutput,
            bool randomSeedEnable)
        {
            _verifyOutput = verifyOutput;
            _randomSeedEnable = randomSeedEnable;

            LogItFunction("Creating Hastlayer Factory...");

            // No "using" here since we are returning this object.
            var hastlayer = Hastlayer.Create();

            hastlayer.ExecutedOnHardware += (sender, e) =>
            LogItFunction("Hastlayer timer: " +
                    e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds + "ms (net) / " +
                    e.HardwareExecutionInformation.FullExecutionTimeMilliseconds + " ms (total)"
                );

            var configuration = new HardwareGenerationConfiguration(
                hastlayer.GetSupportedDevices().First().Name,
                "HardwareFramework");
            configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;
            configuration.EnableCaching = false;

            LogItFunction("Generating hardware...");

            if (_kpzTarget.HastlayerParallelizedAlgorithm())
            {
                configuration.AddHardwareEntryPointType<KpzKernelsParallelizedInterface>();
                configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomMwc64X>(r => r.NextUInt32());
            }
            else if (_kpzTarget.HastlayerPlainAlgorithm())
            {
                configuration.AddHardwareEntryPointType<KpzKernelsInterface>();
            }
            else
            {
                configuration.AddHardwareEntryPointType<PrngTestInterface>();
            }

            LogItFunction("Generating proxy...");

            if (_kpzTarget.HastlayerOnFpga())
            {
                await GenerateProxyAsync(hastlayer, configuration);
            }
            else
            {
                Kernels = new KpzKernelsInterface();
                KernelsParallelized = new KpzKernelsParallelizedInterface();
                LogItFunction("Simulation target detected");
            }

            if (_kpzTarget.HastlayerPlainAlgorithm())
            {
                LogItFunction("Running TestAdd...");
                uint resultFpga = Kernels.TestAddWrapper(hastlayer, configuration, 4_313, 123);
                const uint resultCpu = 4_313 + 123;
                LogItFunction(resultFpga == resultCpu ? $"Success: {resultFpga} == {resultCpu}" : $"Fail: {resultFpga} != {resultCpu}");
            }

            if (_kpzTarget != KpzTarget.PrngTest) return (hastlayer, configuration);
            LogItFunction("Running TestPrng...");

            var kernelsCpu = new PrngTestInterface();
            const ulong randomSeed = 0x37a9_2d76_a96e_f210;
            var smCpu = kernelsCpu.PushRandomSeed(randomSeed, null, null);
            var smFpga = KernelsP.PushRandomSeed(randomSeed, hastlayer, configuration);
            LogItFunction("PRNG results:");
            bool success = true;

            for (int prngTestIndex = 0; prngTestIndex < 10; prngTestIndex++)
            {
                uint prngCpuResult = kernelsCpu.GetNextRandom(smCpu);
                uint prngFpgaResult = KernelsP.GetNextRandom(smFpga);
                if (prngCpuResult != prngFpgaResult) { success = false; }
                LogItFunction($"{prngCpuResult}, {prngFpgaResult}");
            }

            LogItFunction(success ? "TestPrng succeeded!" : "TestPrng failed!");

            return (hastlayer, configuration);
        }

        private async Task GenerateProxyAsync(IHastlayer hastlayer, IHardwareGenerationConfiguration configuration)
        {
            var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
                new[]
                {
                    typeof(KpzKernelsParallelizedInterface).Assembly,
                    typeof(RandomMwc64X).Assembly,
                },
                configuration);

            var proxyConf = new ProxyGenerationConfiguration
            {
                VerifyHardwareResults = _verifyOutput,
            };

            if (_kpzTarget == KpzTarget.Fpga)
            {
                Kernels = await hastlayer.GenerateProxyAsync(
                    hardwareRepresentation,
                    new KpzKernelsInterface(),
                    proxyConf);
            }
            else if (_kpzTarget == KpzTarget.FpgaParallelized)
            {
                KernelsParallelized = await hastlayer.GenerateProxyAsync(
                    hardwareRepresentation,
                    new KpzKernelsParallelizedInterface(),
                    proxyConf);
            }
            else
            {
                KernelsP = await hastlayer.GenerateProxyAsync(
                    hardwareRepresentation,
                    new PrngTestInterface(),
                    proxyConf);
            }

            LogItFunction("FPGA target detected");
        }
    }
}
