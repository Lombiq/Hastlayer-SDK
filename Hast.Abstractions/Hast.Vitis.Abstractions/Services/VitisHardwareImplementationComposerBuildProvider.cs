﻿using CliWrap.EventStream;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (!(context.DeviceManifest is XilinxDeviceManifest deviceManifest))
            {
                throw new InvalidCastException($"The device manifest must be {nameof(XilinxDeviceManifest)} for " +
                                               $"{nameof(VitisHardwareImplementationComposerBuildProvider)} to work.");
            }

            if (string.IsNullOrEmpty(deviceManifest.TechnicalName))
            {
                throw new InvalidOperationException($"The device manifest for '{deviceManifest.Name}' is missing " +
                                                    $"its technical name which is required to build.");
            }

            if (Environment.GetEnvironmentVariable("XILINX_VITIS") == null)
            {
                throw new InvalidOperationException("XILINX_VITIS variable is not set.");
            }

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();

            // Using the variable names in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw" : "hw_emu";
            var device = Directory.GetDirectories("/opt/xilinx/platforms", $"{deviceManifest.TechnicalName}*")
                .OrderByDescending(directoryName => directoryName)
                .First();

            await BuildKernelAsync(hardwareFrameworkPath, target, device, deviceManifest.ClockFrequencyHz / 1_000_000);
            CopyBinaries(hardwareFrameworkPath, target, buildPath, openClConfiguration);

            // TODO:
            // - error handling (?)
            // - interpret performance metrics
            // - cleanup
            throw new NotImplementedException();
        }


        private void CopyBinaries(
            string hardwareFrameworkPath,
            string target,
            string binaryPath,
            IOpenClConfiguration openClConfiguration)
        {
            var xclbinDirectoryPath = GetXclbinDirectoryPath(hardwareFrameworkPath);

            var binaryDirectoryPath = Path.GetDirectoryName(binaryPath);
            if (binaryDirectoryPath != null && !Directory.Exists(binaryDirectoryPath))
            {
                Directory.CreateDirectory(binaryDirectoryPath);
            }

            File.Copy(Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin"), binaryPath);
            openClConfiguration.BinaryFilePath = binaryPath;
        }

        private async Task BuildKernelAsync(string hardwareFrameworkPath, string target, string device, uint frequency)
        {
            var xclbinDirectoryPath = GetXclbinDirectoryPath(hardwareFrameworkPath);
            if (!Directory.Exists(xclbinDirectoryPath)) Directory.CreateDirectory(xclbinDirectoryPath);

            var xoFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xo");

            // For example:
            // vivado -mode batch -source ./HardwareFramework/src/scripts/gen_xo.tcl
            //        -tclargs ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xo hastip hw_emu xilinx_u200_xdma_201830_2
            var vivadoExecutable = (await GetExecutablePathAsync("vivado"));
            var vivadoArguments = new []
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "src", "scripts", "gen_xo.tcl"),
                "-tclargs",
                xoFilePath,
                target,
                device,
            };
            await CliHelper.StreamAsync(vivadoExecutable, vivadoArguments, OnCommandEvent);


            // For example:
            // v++ -R2 -g -t hw_emu --platform xilinx_u200_xdma_201830_2 --save-temps --kernel_frequency 300 -lo
            //     ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xclbin ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xo
            var vppExecutable = (await GetExecutablePathAsync("v++"));
            var vppArguments = new []
            {
                "-R2",
                "-g",
                "-t",
                target,
                "--platform",
                device,
                "--save-temps",
                "--report",
                "estimate",
                "--profile_kernel",
                "data:all:all:all",
                "--kernel_frequency",
                frequency.ToString(CultureInfo.InvariantCulture),
                "-lo",
                Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin"),
                xoFilePath,
            };
            await CliHelper.StreamAsync(vppExecutable, vppArguments, OnCommandEvent);

            // For example:
            // emconfigutil --platform xilinx_u200_xdma_201830_2 --od ./HardwareFramework/rtl/xclbin/
            var emConfigExecutable = (await GetExecutablePathAsync("emconfigutil"));
            var emConfigArguments = new []
            {
                "--platform",
                device,
                "--od",
                xclbinDirectoryPath,
            };
            await CliHelper.StreamAsync(emConfigExecutable, emConfigArguments, OnCommandEvent);
        }

        private void OnCommandEvent(CommandEvent commandEvent)
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
                case ExitedCommandEvent _:
                    // CliMon should do something like this on its own?
                    // if (exited.ExitCode != 0) throw new InvalidOperationException("vivado exited with code " + exited.ExitCode);
                    _logger.LogInformation("Vivado finished execution.");
                    break;
            }
        }

        private string GetXclbinDirectoryPath(string hardwareFrameworkPath) =>
            Path.Combine(hardwareFrameworkPath, "rtl", "xclbin");

        private static async Task<string> GetExecutablePathAsync(string executable)
        {
            var executableName = (await CliHelper.WhichAsync(executable))
                .FirstOrDefault(fileInfo => fileInfo.Exists)?
                .FullName;
            if (executableName == null)
            {
                throw new FileNotFoundException($"The executable '{executable}' was not found. Is it in your PATH?");
            }

            return executableName;
        }
    }
}
