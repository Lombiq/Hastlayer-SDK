using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.ManifestProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Xilinx.Abstractions
{
    public class VivadoHardwareImplementationComposer : IHardwareImplementationComposer
    {
        public ILogger Logger { get; set; }


        public VivadoHardwareImplementationComposer()
        {
            Logger = NullLogger.Instance;
        }


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription &&
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public Task<IHardwareImplementation> Compose(IHardwareImplementationCompositionContext context)
        {
            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var deviceManifest = context.DeviceManifest;
            var vhdlHardwareDescription = (VhdlHardwareDescription)context.HardwareDescription;

            if (string.IsNullOrEmpty(hardwareFrameworkPath))
            {
                Logger.LogWarning("No hardware framework path was configured. Thus while the hardware description was created it won't be implemented with the FPGA vendor toolchain.");
                return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
            }


            var isNexys = deviceManifest.Name == Nexys4DdrManifestProvider.DeviceName ||
                deviceManifest.Name == NexysA7ManifestProvider.DeviceName;


            CreateDirectoryIfDoesntExist(hardwareFrameworkPath);


            string vhdlFilePath;
            if (isNexys)
            {
                CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "IPRepo"));
                vhdlFilePath = Path.Combine(hardwareFrameworkPath, "IPRepo", "Hast_IP.vhd");
            }
            else
            {
                CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "src"));
                CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "src", "IP"));
                vhdlFilePath = Path.Combine(hardwareFrameworkPath, "src", "IP", "Hast_IP.vhd");
            }

            File.WriteAllText(vhdlFilePath, vhdlHardwareDescription.VhdlSource);


            string xdcFileSubPath;

            if (isNexys) xdcFileSubPath = "Nexys4DDR_Master.xdc";
            else xdcFileSubPath = Path.Combine("src", "IP", "Hast_IP.xdc");

            var xdcFilePath = Path.Combine(hardwareFrameworkPath, xdcFileSubPath);
            var xdcFileTemplatePath = xdcFilePath + "_template";

            if (File.Exists(xdcFilePath))
            {
                // Using the original XDC file as a template and then adding constraints to it.
                if (!File.Exists(xdcFileTemplatePath))
                {
                    File.Copy(xdcFilePath, xdcFileTemplatePath);
                }

                File.Copy(xdcFileTemplatePath, xdcFilePath, true);
            }

            if (!string.IsNullOrEmpty(vhdlHardwareDescription.XdcSource))
            {
                File.AppendAllText(xdcFilePath, vhdlHardwareDescription.XdcSource);
            }
            else if (File.Exists(xdcFileTemplatePath))
            {
                // The XDC file can contain constraints of previous hardware designs so clearing those out.
                File.Copy(xdcFileTemplatePath, xdcFilePath, true);
            }


            return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
        }


        private static void CreateDirectoryIfDoesntExist(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }
    }
}
