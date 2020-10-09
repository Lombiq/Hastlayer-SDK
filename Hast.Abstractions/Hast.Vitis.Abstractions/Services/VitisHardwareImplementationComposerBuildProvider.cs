using CliWrap.EventStream;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public class VitisHardwareImplementationComposerBuildProvider : IHardwareImplementationComposerBuildProvider
    {
        private readonly ILogger _logger;

        public IEnumerable<string> SupportedComposers { get; } = new[] { nameof(VivadoHardwareImplementationComposer) };


        public VitisHardwareImplementationComposerBuildProvider(
            ILogger<VitisHardwareImplementationComposerBuildProvider> logger) =>
            _logger = logger;


        public bool IsSupported(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.DeviceType == XilinxDeviceType.Vitis;

        public async Task<IHardwareImplementation> BuildAsync(
            IHardwareImplementationCompositionContext context,
            string buildPath)
        {
            var deviceManifest = (XilinxDeviceManifest)context.DeviceManifest;
            if (string.IsNullOrEmpty(deviceManifest?.TechnicalName))
            {
                throw new InvalidOperationException($"The device manifest for '{deviceManifest?.Name}' is missing " +
                                                    $"its technical name which is required to build.");
            }

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();
            openClConfiguration.BinaryFilePath = buildPath;

            // using the names found in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw" : "hw_emu";
            var device = Directory.GetDirectories("/opt/xilinx/platforms", $"{deviceManifest.TechnicalName}*")
                .OrderByDescending(directoryName => directoryName)
                .First();

            // vivado -mode batch -source ./HardwareFramework/src/scripts/gen_xo.tcl -tclargs ./xclbin/hastip.hw_emu.xo hastip hw_emu xilinx_u200_xdma_201830_2
            var vivadoExecutable = (await CliHelper.WhichAsync("vivado")).FirstOrDefault(fileInfo => fileInfo.Exists);
            if (vivadoExecutable == null) throw new FileNotFoundException("The executable 'vivado' was not found. Is it in your PATH?");

            var vivadoArguments = new []
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "src", "scripts", "gen_xo.tcl"),
                "-tclargs",
                Path.Combine(hardwareFrameworkPath, "rtl", "xclbin", $"hastip.{target}.xo"),
                target,
                device,
            };
            await CliHelper.StreamAsync(vivadoExecutable.FullName, vivadoArguments, VivadoOnCommandEvent);

            // TODO:
            // - v++,
            // - error handling
            // - copy and rename built files
            // - interpret performance metrics
            // - cleanup
            throw new NotImplementedException();
        }


        private void VivadoOnCommandEvent(CommandEvent commandEvent)
        {
            switch (commandEvent)
            {
                case StartedCommandEvent started:
                    _logger.LogInformation("Launching Vivado. (process ID: {0})", started.ProcessId);
                    break;
                case StandardOutputCommandEvent output:
                    _logger.LogTrace("Vivado: {0}", output.Text);
                    break;
                case StandardErrorCommandEvent error:
                    _logger.LogWarning("Vivado: {0}", error.Text);
                    break;
                case ExitedCommandEvent exited:
                    // CliMon should do something like this on its own?
                    // if (exited.ExitCode != 0) throw new InvalidOperationException("vivado exited with code " + exited.ExitCode);
                    _logger.LogInformation("Vivado finished execution.");
                    break;
            }
        }
    }
}
