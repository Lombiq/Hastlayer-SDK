using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions.ManifestProviders;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Xilinx.Abstractions
{
    public class VivadoHardwareImplementationComposer : IHardwareImplementationComposer
    {
        public Task<IHardwareImplementation> Compose(
            IHardwareGenerationConfiguration configuration,
            IHardwareDescription hardwareDescription,
            IDeviceManifest deviceManifest)
        {
            var hardwareFrameworkPath = configuration.HardwareFrameworkPath;

            if (string.IsNullOrEmpty(hardwareFrameworkPath))
            {
                // Log: No hardware framework path was configured. Thus while the hardware description was created it won't be implemented with the FPGA vendor toolchain.
                return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
            }

            if (!(hardwareDescription is VhdlHardwareDescription vhdlHardwareDescription))
            {
                throw new NotSupportedException("The given hardware description needs to be of type VhdlHardwareDescription.");
            }

            if (deviceManifest.ToolChainName != CommonToolChainNames.Vivado)
            {
                throw new InvalidOperationException("Only the Vivado toolchain is supported by this hardware implementation composer.");
            }


            var isNexys = deviceManifest.Name == Nexys4DdrManifestProvider.DeviceName ||
                deviceManifest.Name == NexysA7ManifestProvider.DeviceName;


            CreateDirectoryIfDoesntExist(hardwareFrameworkPath);


            string vhdlFileSubPath;
            if (isNexys)
            {
                CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "IPRepo"));
                vhdlFileSubPath = Path.Combine(hardwareFrameworkPath, "IPRepo", "Hast_IP.vhd");
            }
            else vhdlFileSubPath = Path.Combine(hardwareFrameworkPath, "Hast_IP.vhd");

            File.WriteAllText(vhdlFileSubPath, vhdlHardwareDescription.VhdlSource);


            string xdcFileSubPath;

            if (isNexys) xdcFileSubPath = "Nexys4DDR_Master.xdc";
            else xdcFileSubPath = "Hast_IP.xdc";

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

        public class HardwareImplementation : IHardwareImplementation
        {
        }
    }
}
