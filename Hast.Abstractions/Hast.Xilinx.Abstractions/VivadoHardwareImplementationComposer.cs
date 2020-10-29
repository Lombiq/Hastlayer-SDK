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
        private readonly IEnumerable<IHardwareImplementationComposerBuildProvider> _buildProviders;


        public VivadoHardwareImplementationComposer(
            ILogger<VivadoHardwareImplementationComposer> logger,
            IEnumerable<IHardwareImplementationComposerBuildProvider> buildProviders)
        {
            _logger = logger;
            _buildProviders = buildProviders;
        }


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription &&
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public async Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context)
        {
            if (!(context.DeviceManifest is XilinxDeviceManifest deviceManifest))
            {
                throw new InvalidCastException(
                    $"This composer expects a {nameof(XilinxDeviceManifest)} because Vivado works with Xilinx FPGAs.");
            }

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            if (string.IsNullOrEmpty(hardwareFrameworkPath))
            {
                _logger.LogWarning(
                    "No hardware framework path was configured. Thus while the hardware description was created it " +
                    "won't be implemented with the FPGA vendor toolchain.");
                return new HardwareImplementation();
            }

            var hashId = context.HardwareDescription.TransformationId;
            var vhdlFilePath = GetFilePath(deviceManifest, hardwareFrameworkPath, hashId);
            var name = context.Configuration.Label;
            if (!string.IsNullOrWhiteSpace(name)) File.WriteAllText(vhdlFilePath + ".name", name);
            var hashFile = vhdlFilePath + ".hash";
            if (!File.Exists(hashFile) || File.ReadAllText(hashFile).Trim() != hashId)
            {
                CreateFiles(context, deviceManifest, vhdlFilePath, hashId);
                File.WriteAllText(hashFile, hashId);
            }

            var implementation = new HardwareImplementation
            {
                BinaryPath = deviceManifest.DeviceType switch
                {
                    XilinxDeviceType.Vitis =>
                        Path.Combine(
                            CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "bin"),
                            hashId + ".xclbin"),
                    XilinxDeviceType.Nexys => null,
                    _ => throw deviceManifest.GetUnknownDeviceType(),
                },
            };

            foreach (var buildProvider in _buildProviders
                .Where(provider => provider.IsSupported(context, implementation)))
            {
                await buildProvider.BuildAsync(context, implementation);
            }

            return implementation;
        }


        private static void CreateFiles(
            IHardwareImplementationCompositionContext context,
            XilinxDeviceManifest deviceManifest,
            string vhdlFilePath,
            string hashId)
        {
            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var vhdlHardwareDescription = (VhdlHardwareDescription)context.HardwareDescription;

            if (deviceManifest.DeviceType == XilinxDeviceType.Vitis)
            {
                // Copy templates from ./HardwareFramework/rtl/src to the execution specific directory.
                CopyAll(
                    new DirectoryInfo(Path.Combine(hardwareFrameworkPath, "rtl", "src", "IP")),
                    new DirectoryInfo(Path.Combine(hardwareFrameworkPath, "rtl", hashId, "src", "IP")));
            }

            CreateDirectoryIfDoesntExist(Path.GetDirectoryName(vhdlFilePath));
            File.WriteAllText(vhdlFilePath, vhdlHardwareDescription.VhdlSource);

            string xdcFileSubPath = deviceManifest.DeviceType switch
            {
                XilinxDeviceType.Vitis => Path.Combine("rtl", hashId, "src", "IP", "Hast_IP.xdc"),
                XilinxDeviceType.Nexys => "Nexys4DDR_Master.xdc",
                _ => throw new InvalidOperationException($"Unknown device type: {deviceManifest.DeviceType}.")
            };

            var xdcFilePath = Path.Combine(hardwareFrameworkPath, xdcFileSubPath);
            var xdcFileTemplatePath = xdcFilePath + "_template";
            CreateDirectoryIfDoesntExist(Path.GetDirectoryName(xdcFilePath));

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

        private static string GetFilePath(XilinxDeviceManifest deviceManifest, string hardwareFrameworkPath, string hashId)
        {
            string directory = deviceManifest.DeviceType switch
            {
                XilinxDeviceType.Nexys => CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "IPRepo"),
                XilinxDeviceType.Vitis => CreateDirectoryIfDoesntExist(hardwareFrameworkPath, "rtl", hashId, "src", "IP"),
                _ => throw deviceManifest.GetUnknownDeviceType(),
            };

            return Path.Combine(directory, "Hast_IP.vhd");
        }

        private static string CreateDirectoryIfDoesntExist(params string[] pathComponents)
        {
            var path = Path.Combine(pathComponents);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        // Source: https://stackoverflow.com/a/690980
        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (var file in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, file.Name);
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var subDirectory in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(subDirectory.Name);
                CopyAll(subDirectory, nextTargetSubDir);
            }
        }
    }
}
