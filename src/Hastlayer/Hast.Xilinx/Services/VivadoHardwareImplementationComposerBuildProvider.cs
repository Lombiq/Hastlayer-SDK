﻿using Hast.Layer;
using Hast.Synthesis.Delegates;
using Hast.Synthesis.Helpers;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Lombiq.HelpfulLibraries.Common.Utilities.FileSystemHelper;

namespace Hast.Xilinx.Services;

public class VivadoHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
{
    public IDictionary<string, BuildProviderShortcut> Shortcuts { get; } = new Dictionary<string, BuildProviderShortcut>();

    public bool CanCompose(IHardwareImplementationCompositionContext context) =>
        context.DeviceManifest is NexysDeviceManifest;

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

    public void InvokeProgress(BuildProgressEventArgs eventArgs)
    {
        // There are no progress steps for Vivado.
    }
}
