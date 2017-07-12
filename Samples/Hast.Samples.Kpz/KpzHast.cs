using Hast.Layer;
using System;
using System.IO;
using System.Threading.Tasks;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System.Linq;

namespace Hast.Samples.Kpz
{
    public partial class Kpz
    {
        public string VhdlOutputFilePath = @"Hast_IP.vhd";
        public delegate void LogItDelegate(string toLog);
        public LogItDelegate LogItFunction; //Should be AsyncLogIt from ChartForm
        public KpzKernelsInterface Kernels;
        private bool _verifyOutput;

        public async Task InitializeHastlayer(bool verifyOutput)
        {
            _verifyOutput = verifyOutput;

            LogItFunction("Creating Hastlayer Factory...");
            var hastlayer = await Hastlayer.Create();
            hastlayer.ExecutedOnHardware += (sender, e) =>
            {
                LogItFunction("Hastlayer timer: " +
                    e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds + "ms (net) / " +
                    e.HardwareExecutionInformation.FullExecutionTimeMilliseconds + " ms (total)"
                );
            };

            var configuration = new HardwareGenerationConfiguration((await hastlayer.GetSupportedDevices()).First().Name);
            configuration.HardwareEntryPointMemberNamePrefixes.Add("Hast.Samples.Kpz.KpzKernels");
            configuration.VhdlTransformerConfiguration().VhdlGenerationMode = VhdlGenerationMode.Debug;
            configuration.EnableCaching = false;

            LogItFunction("Generating hardware...");
            var hardwareRepresentation = await hastlayer.GenerateHardware( new[] {
                typeof(KpzKernelsInterface).Assembly,
             //   typeof(Hast.Algorithms.MWC64X).Assembly
            }, configuration);

            await hardwareRepresentation.HardwareDescription.WriteSource(VhdlOutputFilePath);

            LogItFunction("Generating proxy...");
            if (kpzTarget == KpzTarget.Fpga)
            {
                ProxyGenerationConfiguration proxyConf = new ProxyGenerationConfiguration();
                proxyConf.ValidateHardwareResults = _verifyOutput;
                Kernels = await hastlayer.GenerateProxy<KpzKernelsInterface>(hardwareRepresentation, new KpzKernelsInterface(), proxyConf);
                LogItFunction("FPGA target detected");
            }
            else //if (kpzTarget == KpzTarget.FPGASimulation)
            {
                Kernels = new KpzKernelsInterface();
                LogItFunction("Simulation target detected");
            }

            LogItFunction("Running TestAdd...");
            uint resultFpga = Kernels.TestAddWrapper(4313,123);
            uint resultCpu  = 4313+123;
            if(resultCpu == resultFpga) LogItFunction(String.Format("Success: {0} == {1}", resultFpga, resultCpu));
            else LogItFunction(String.Format("Fail: {0} != {1}", resultFpga, resultCpu));

            /*
            LogItFunction("Running TestPrng...");
            KpzKernelsInterface KernelsCpu = new KpzKernelsInterface();
            uint[] resultPrngFpga = Kernels.TestPrngWrapper();
            uint[] resultPrngCpu = KernelsCpu.TestPrngWrapper();
            string numbersFpga = "", numbersCpu = "";
            for (int i = 0; i < 10; i++)
            {
                numbersCpu += resultPrngCpu[i].ToString() + " ";
                numbersFpga += resultPrngFpga[i].ToString() + " ";
            }
            Console.WriteLine("TestPrng CPU results: " + numbersCpu);
            if (kpzTarget != KpzTarget.FpgaSimulation) Console.WriteLine("TestPrng FPGA results: " + numbersFpga);
            LogItFunction("Done.");
            */
        }
    }
}
