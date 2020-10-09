using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Xilinx.Abstractions
{
    public class VivadoHardwareImplementationComposer : IHardwareImplementationComposer
    {
        private readonly ILogger _logger;
        private readonly IList<IHardwareImplementationComposerBuildProvider> _buildProviders;


        public VivadoHardwareImplementationComposer(
            ILogger<VivadoHardwareImplementationComposer> logger,
            IEnumerable<IHardwareImplementationComposerBuildProvider> buildProviders)
        {
            _logger = logger;
            _buildProviders = buildProviders
                .Where(x => x.SupportedComposers.Contains(nameof(VivadoHardwareImplementationComposer)))
                .ToList();
        }


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription &&
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public async Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context)
        {
            if (!(context.DeviceManifest is XilinxDeviceManifest deviceManifest))
            {
                throw new InvalidCastException($"This composer expects a {nameof(XilinxDeviceManifest)} type " +
                                               "manifest because Vivado works with Xilinx FPGAs.");
            }

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            if (string.IsNullOrEmpty(hardwareFrameworkPath))
            {
                _logger.LogWarning("No hardware framework path was configured. Thus while the hardware description " +
                                   "was created it won't be implemented with the FPGA vendor toolchain.");
                return new HardwareImplementation();
            }

            var (vhdlDirectory, vhdlFileName) = GetFilePath(deviceManifest, hardwareFrameworkPath);
            var vhdlFilePath = Path.Combine(vhdlDirectory, vhdlFileName);
            var hashId = context.HardwareDescription.TransformationId;
            var hashFile = vhdlFilePath + ".hash";
            if (!File.Exists(hashFile) ||
                File.ReadAllText(hashFile).Trim() != hashId)
            {
                CreateFiles(context, deviceManifest, vhdlFilePath);
                File.WriteAllText(hashFile, hashId);
            }

            if (deviceManifest.DeviceType == XilinxDeviceType.Vitis)
            {

                var vhdlBinaryPath = Path.Combine(
                    CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "rtl", "xclbin"),
                    hashId + ".xclbin");
                if (!File.Exists(vhdlBinaryPath) &&
                    _buildProviders.FirstOrDefault(provider => provider.IsSupported(context)) is {} buildProvider)
                {
                    await buildProvider.BuildAsync(context, vhdlBinaryPath);
                }
            }

            return new HardwareImplementation();
        }


        private static void CreateFiles(
            IHardwareImplementationCompositionContext context,
            XilinxDeviceManifest deviceManifest,
            string vhdlFilePath)
        {
            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var vhdlHardwareDescription = (VhdlHardwareDescription)context.HardwareDescription;

            File.WriteAllText(vhdlFilePath, vhdlHardwareDescription.VhdlSource);

            var xdcFileSubPath = deviceManifest.DeviceType switch
            {
                XilinxDeviceType.Vitis => Path.Combine("rtl", "src", "IP", "Hast_IP.xdc"),
                XilinxDeviceType.Nexys => "Nexys4DDR_Master.xdc",
                _ => throw new InvalidOperationException($"Unknown device type: {deviceManifest.DeviceType}"),
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

        private static (string Directory, string FileName) GetFilePath(
            XilinxDeviceManifest deviceManifest,
            string hardwareFrameworkPath)
        {
            string directory;
            switch (deviceManifest.DeviceType)
            {
                case XilinxDeviceType.Nexys:
                    directory = CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "IPRepo");
                    break;
                case XilinxDeviceType.Vitis:
                    directory = CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "rtl", "src", "IP");
                    break;
                default: throw new InvalidOperationException($"Unknown device type: {deviceManifest.DeviceType}");
            }

            return (directory, "Hast_IP.vhd");
        }

        private static string CreateDirectoryIfDoesntExist(params string[] pathComponents)
        {
            var path = Path.Combine(pathComponents);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}
