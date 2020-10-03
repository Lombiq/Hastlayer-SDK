using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Xilinx.Abstractions
{
    public class VivadoHardwareImplementationComposer : IHardwareImplementationComposer
    {
        private readonly ILogger _logger;


        public VivadoHardwareImplementationComposer(ILogger<VivadoHardwareImplementationComposer> logger) =>
            _logger = logger;


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription &&
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context)
        {
            if (!(context.DeviceManifest is XilinxDeviceManifest deviceManifest))
            {
                throw new InvalidCastException($"This composer expects a {nameof(XilinxDeviceManifest)} type " +
                                               "manifest because Vivado works with Xilinx FPGAs.");
            }
            if (string.IsNullOrEmpty(context.Configuration.HardwareFrameworkPath))
            {
                _logger.LogWarning("No hardware framework path was configured. Thus while the hardware description " +
                                   "was created it won't be implemented with the FPGA vendor toolchain.");
                return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
            }

            CreateFiles(context, deviceManifest);

            return Task.FromResult((IHardwareImplementation)new HardwareImplementation());
        }


        private static void CreateFiles(
            IHardwareImplementationCompositionContext context,
            XilinxDeviceManifest deviceManifest)
        {
            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var vhdlHardwareDescription = (VhdlHardwareDescription)context.HardwareDescription;

            CreateDirectoryIfDoesntExist(hardwareFrameworkPath);
            File.WriteAllText(GetFilePath(deviceManifest, hardwareFrameworkPath), vhdlHardwareDescription.VhdlSource);


            var xdcFileSubPath = deviceManifest.DeviceType switch
            {
                XilinxDeviceType.Vitis => Path.Combine("rtl", "src", "IP", "Hast_IP.xdc"),
                XilinxDeviceType.Nexys => "Nexys4DDR_Master.xdc",
                _ => throw new InvalidOperationException($"Unknown device type: {deviceManifest.DeviceType}")
            };
            var xdcFilePath = Path.Combine(hardwareFrameworkPath, xdcFileSubPath);
            var xdcFileTemplatePath = xdcFilePath + "_template";

            // Using the original XDC file as a template and then adding constraints to it.
            if (File.Exists(xdcFilePath) && !File.Exists(xdcFileTemplatePath))
            {
                File.Copy(xdcFilePath, xdcFileTemplatePath);
            }
            else if (File.Exists(xdcFileTemplatePath))
            {
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
        }

        private static void CreateDirectoryIfDoesntExist(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }

        private static string GetFilePath(XilinxDeviceManifest deviceManifest, string hardwareFrameworkPath)
        {
            switch (deviceManifest.DeviceType)
            {
                case XilinxDeviceType.Nexys:
                    CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "IPRepo"));
                    return Path.Combine(hardwareFrameworkPath, "IPRepo", "Hast_IP.vhd");
                case XilinxDeviceType.Vitis:
                    CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "rtl"));
                    CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "rtl", "src"));
                    CreateDirectoryIfDoesntExist(Path.Combine(hardwareFrameworkPath, "rtl", "src", "IP"));
                    return Path.Combine(hardwareFrameworkPath, "rtl", "src", "IP", "Hast_IP.vhd");
                default: return string.Empty;
            }
        }
    }
}
