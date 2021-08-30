using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;

namespace Hast.Vitis.Abstractions.Services
{
    public class VivadoHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        public Dictionary<string, BuildProviderShortcut> Shortcuts { get; } = new();

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vivado;

        public Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            return VhdlHelper.CreateVhdlAndXdcFilesAsync(
                context,
                Path.Combine(hardwareFrameworkPath, "Nexys4DDR_Master.xdc"),
                Path.Combine(EnsureDirectoryExists(hardwareFrameworkPath, "IPRepo"), "Hast_IP.vhd"));
        }

        public void InvokeProgress(BuildProgressEventArgs eventArgs) { }
    }
}
