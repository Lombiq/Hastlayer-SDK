﻿using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Helpers;
using System.IO;
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;

namespace Hast.Vitis.Abstractions.Services
{
    public class VivadoHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            VhdlHelper.CreateVhdlAndXdcFiles(
                context,
                Path.Combine(EnsureDirectoryExists(hardwareFrameworkPath, "IPRepo"), "Hast_IP.vhd"),
                Path.Combine(hardwareFrameworkPath, "Nexys4DDR_Master.xdc"));

            return Task.CompletedTask;
        }
    }
}
