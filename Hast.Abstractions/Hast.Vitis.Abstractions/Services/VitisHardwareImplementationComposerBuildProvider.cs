using CliWrap.EventStream;
using CliWrap.Exceptions;
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
using System.Text;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public sealed class VitisHardwareImplementationComposerBuildProvider
        : IHardwareImplementationComposerBuildProvider, IDisposable
    {
        public const int StepsTotal = 9;
        private const string InfoFileExtension = OpenClCommunicationService.InfoFileExtension;

        private readonly ILogger _logger;
        private readonly string _buildOutputPath;
        private readonly StreamWriter _buildOutput;

        public event EventHandler<string> Progress;

        public int Step { get; private set; }
        public IEnumerable<string> SupportedComposers { get; } = new[] { nameof(VivadoHardwareImplementationComposer) };


        public VitisHardwareImplementationComposerBuildProvider(
            ILogger<VitisHardwareImplementationComposerBuildProvider> logger)
        {
            Progress += OnProgress;
            _logger = logger;

            var buildOutputPath = Path.Combine("App_Data", "logs");
            if (!Directory.Exists(buildOutputPath)) Directory.CreateDirectory(buildOutputPath);
            _buildOutputPath = Path.Combine(buildOutputPath, "build.out");
            _buildOutput = new StreamWriter(_buildOutputPath, append: false, Encoding.UTF8);
        }


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
                                                    "its technical name which is required to build.");
            }

            if (Environment.GetEnvironmentVariable("XILINX_VITIS") == null)
            {
                throw new InvalidOperationException("XILINX_VITIS variable is not set.");
            }

            Progress!(this, "Environment ready.");

            var hardwareFrameworkPath = context.Configuration.HardwareFrameworkPath;
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();
            Cleanup(hardwareFrameworkPath);

            // Using the variable names in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw_emu" : "hw";
            var device = Directory.GetDirectories("/opt/xilinx/platforms", $"{deviceManifest.TechnicalName}*")
                .Select(Path.GetFileName)
                .OrderByDescending(directoryName => directoryName)
                .First();

            Progress!(this, "Staring Build.");
            await BuildKernelAsync(hardwareFrameworkPath, target, device, deviceManifest.ClockFrequencyHz / 1_000_000);
            CopyBinaries(hardwareFrameworkPath, target, buildPath, openClConfiguration);

            // TODO:
            // - error handling (?)
            // - interpret performance metrics
            throw new NotImplementedException();
        }


        private async Task BuildKernelAsync(string hardwareFrameworkPath, string target, string device, uint frequency)
        {
            var xclbinDirectoryPath = GetXclbinDirectoryPath(hardwareFrameworkPath);
            if (!Directory.Exists(xclbinDirectoryPath)) Directory.CreateDirectory(xclbinDirectoryPath);

            var xoFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xo");

            // For example:
            // vivado -mode batch -source ./HardwareFramework/rtl/src/scripts/gen_xo.tcl
            //        -tclargs ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xo hastip hw_emu xilinx_u200_xdma_201830_2
            var vivadoExecutable = (await GetExecutablePathAsync("vivado"));
            var vivadoArguments = new []
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", "gen_xo.tcl"),
                "-tclargs",
                xoFilePath,
                "hastip",
                target,
                device,
            };
            await ExecuteWithLogging(vivadoExecutable, vivadoArguments);
            Progress!(this, "Vivado build is finished.");


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
                //"--profile_kernel",
                //"data:all:all:all",
                "--kernel_frequency",
                frequency.ToString(CultureInfo.InvariantCulture),
                "-lo",
                Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin"),
                xoFilePath,
            };
            await ExecuteWithLogging(vppExecutable, vppArguments);
            Progress!(this, "v++ build is finished.");

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
            await ExecuteWithLogging(emConfigExecutable, emConfigArguments);
            Progress!(this, "Emulation configuration (emconfig) setup is finished.");
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

            var builtFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin");
            File.Copy(builtFilePath, binaryPath);
            File.Copy(builtFilePath + InfoFileExtension, binaryPath + InfoFileExtension);
            openClConfiguration.BinaryFilePath = binaryPath;
            Progress!(this, $"Files copied to binary folder ({builtFilePath}).");
        }

        private void Cleanup(string hardwareFrameworkPath)
        {
            var toDelete = Directory.GetDirectories(hardwareFrameworkPath, "tmp_*")
                .Union(Directory.GetDirectories(hardwareFrameworkPath, "packaged_kernel_*"))
                .Union(new[] { "_x", GetXclbinDirectoryPath(hardwareFrameworkPath) })
                .Where(Directory.Exists);

            foreach (var directory in toDelete)
            {
                Directory.Delete(directory, recursive: true);
            }

            // In the makefile it is:
            // rm -rf host ./xclbin/{*sw_emu*,*hw_emu*}
            // rm -rf TempConfig system_estimate.xtxt *.rpt
            // rm -rf src/*.ll _v++_* .Xil emconfig.json dltmp* xmltmp* *.log *.jou
            // rm -rf ./xclbin
            // rm -rf _x.*
            // rm -rf ./tmp_kernel_pack* ./packaged_kernel* _x/
            // TODO: Where is: TempConfig, system_estimate.xtxt, *.rpt, src/*.ll, _v++_*, tmp_*, etc
            Progress!(this, "Build directory cleaned up.");
        }


        private void OnProgress(object sender, string message) =>
            _logger.LogInformation("Build step {0}/{1} completed: {2}", ++Step, StepsTotal, message);

        private Task ExecuteWithLogging(string executable, IList<string> arguments)
        {
            var name = Path.GetFileName(executable);
            void OnCommandEvent(CommandEvent commandEvent)
            {
                switch (commandEvent)
                {
                    case StartedCommandEvent started:
                        _logger.LogInformation("Started {0}. (process ID: {1})", name, started.ProcessId);
                        break;
                    case StandardOutputCommandEvent output:
                        _logger.LogTrace("{0}: {1}", name, output.Text);
                        _buildOutput.WriteLine("{0} stdout: {1}", name, output.Text);
                        break;
                    case StandardErrorCommandEvent error:
                        _logger.LogWarning("{0}: {1}", name, error.Text);
                        _buildOutput.WriteLine("{0} stderr: {1}", name, error.Text);
                        break;
                    case ExitedCommandEvent exited:
                        _buildOutput.WriteLine("{0} exit code: {1}\n\n\n", name, exited.ExitCode);

                        if (exited.ExitCode != 0)
                        {
                            throw new CommandExecutionException(
                                $"The command {name} exited with code {exited.ExitCode}. " +
                                $"You can review the output at '{Path.GetFullPath(_buildOutputPath)}'.");
                        }

                        _logger.LogInformation(name + " finished execution.");
                        break;
                }
            }

            _logger.LogInformation("Starting program: {0} {1}", executable, string.Join(" ", arguments));
            return CliHelper.StreamAsync(executable, arguments, OnCommandEvent);
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

        public void Dispose() => _buildOutput?.Dispose();
    }
}
