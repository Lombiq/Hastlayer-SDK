using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
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
        public const int StepsTotal = 8;
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
            for (var i = 0; i < 100 && _buildOutput == null; i++)
            {
                var fileName = i == 0 ? "build.out" : $"build~{i}.out";
                try
                {
                    _buildOutputPath = Path.Combine(buildOutputPath, fileName);
                    _buildOutput = new StreamWriter(_buildOutputPath, append: false, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to open {0} for writing.", fileName);
                }
            }
        }


        /// <summary>
        /// It is supported if the manifest is a <see cref="XilinxDeviceManifest"/> with the type of
        /// <see cref="XilinxDeviceType.Vitis"/> and the <see cref="IHardwareImplementation.BinaryPath"/> is set but the
        /// indicated file doesn't yet exist.
        /// </summary>
        public bool IsSupported(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation) =>
            context.DeviceManifest is XilinxDeviceManifest xilinxDeviceManifest &&
            xilinxDeviceManifest.DeviceType == XilinxDeviceType.Vitis &&
            !string.IsNullOrEmpty(implementation.BinaryPath) &&
            !File.Exists(implementation.BinaryPath);

        public async Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
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

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XILINX_VITIS")))
            {
                throw new InvalidOperationException("XILINX_VITIS variable is not set.");
            }

            var xilinxDirectoryPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("XILINX_PATH_XRT"));
            if (!Directory.Exists(xilinxDirectoryPath))
            {
                throw new InvalidOperationException(
                    "XILINX_PATH_XRT variable is not set or it is not pointing to an existing directory.");
            }

            Progress!(this, "Environment ready.");

            var hashId = context.HardwareDescription.TransformationId;
            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();
            Cleanup(hardwareFrameworkPath, hashId, deleteSource: false);

            var platformsDirectoryPath = Environment.GetEnvironmentVariable("XILINX_PLATFORM") is { } platformVariable
                ? Path.GetFullPath(platformVariable)
                : Path.Combine(xilinxDirectoryPath, "platforms");

            // Using the variable names in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw_emu" : "hw";
            // Instead of the platform name like xilinx_u200_xdma_201830_2, you can use the full path of the .xpfm file
            // in the platform directory. This way you can override the platform directory by setting $XILINX_PLATFORM.
            // see: https://github.com/Xilinx/Vitis-Tutorials/issues/3
            var device = new DirectoryInfo(platformsDirectoryPath)
                .GetDirectories($"{deviceManifest.TechnicalName}*")
                .SelectMany(directory => directory.GetFiles("*.xpfm").Select(file => file.FullName))
                .OrderByDescending(name => name)
                .First();

            if (context.Configuration.GetOrAddVitisBuildConfiguration().SynthesisOnly)
            {
                await SynthKernelAsync(hardwareFrameworkPath, hashId);
                Environment.Exit(0);
            }

            Progress!(this, "Staring build.");
            await BuildKernelAsync(hardwareFrameworkPath, target, device, hashId, deviceManifest);
            CopyBinaries(hardwareFrameworkPath, target, implementation.BinaryPath, hashId);

            Progress!(this, "Collecting reports.");
            try { CollectReport(hardwareFrameworkPath, context, hashId); }
            catch (Exception e) { _logger.LogError(e, "Failed to collect reports."); }

            Cleanup(hardwareFrameworkPath, hashId);
        }


        private async Task BuildKernelAsync(
            string hardwareFrameworkPath,
            string target,
            string device,
            string hashId,
            XilinxDeviceManifest deviceManifest)
        {
            var xclbinDirectoryPath = GetXclbinDirectoryPath(hardwareFrameworkPath, hashId);
            if (!Directory.Exists(xclbinDirectoryPath)) Directory.CreateDirectory(xclbinDirectoryPath);
            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);

            var xoFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xo");

            // For example:
            // vivado -mode batch -source ./HardwareFramework/rtl/src/scripts/gen_xo.tcl
            //        -tclargs ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xo hastip hw_emu xilinx_u200_xdma_201830_2
            var vivadoExecutable = (await GetExecutablePathAsync("vivado"));
            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", "gen_xo.tcl"),
                "-tclargs",
                xoFilePath,
                target,
                device,
            };
            await ExecuteWithLogging(vivadoExecutable, vivadoArguments, rtlDirectoryPath);
            Progress!(this, "Vivado build is finished.");


            // For example:
            // v++ -R2 -g -t hw_emu --platform xilinx_u200_xdma_201830_2 --save-temps --kernel_frequency 300 -lo
            //     ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xclbin ./HardwareFramework/rtl/xclbin/hastip.hw_emu.xo
            var vppExecutable = (await GetExecutablePathAsync("v++"));
            var vppArguments = new List<string>
            {
                "-R2",
                "-g",
                "-t",
                target,
                "--platform",
                device,
                "--save-temps",
                "--kernel_frequency",
                (deviceManifest.ClockFrequencyHz / 1_000_000).ToString(CultureInfo.InvariantCulture),
            };

            if (deviceManifest.SupportsHbm)
            {
                /* TODO
                vppArguments.AddRange(new[]
                {
                    "--sp",
                    "krnl_vadd_1.m_axi_gmem0:HBM[0:3]",
                }); // */
            }

            vppArguments.AddRange(new[]
            {
                "-lo",
                Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin"),
                xoFilePath,
            });

            await ExecuteWithLogging(vppExecutable, vppArguments, rtlDirectoryPath);
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
            await ExecuteWithLogging(emConfigExecutable, emConfigArguments, rtlDirectoryPath);
            Progress!(this, "Emulation configuration (emconfig) setup is finished.");
        }

        private async Task SynthKernelAsync(string hardwareFrameworkPath, string hashId)
        {
            // vivado -mode batch -source synth_util.tcl
            var vivadoExecutable = (await GetExecutablePathAsync("vivado"));

            var reportsDirectoryPath = Path.Combine(hardwareFrameworkPath, "reports", hashId);
            if (!Directory.Exists(reportsDirectoryPath)) Directory.CreateDirectory(reportsDirectoryPath);

            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", "synth_util.tcl"),
                "-tclargs",
                Path.Combine(hardwareFrameworkPath, "rtl", hashId, "src", "IP", "Hast_IP.vhd"),
                Path.Combine(reportsDirectoryPath, "Hast_IP_synth_util.rpt"),
            };
            await ExecuteWithLogging(vivadoExecutable, vivadoArguments);
            Progress!(this, "Vivado synthesis is finished.");
        }

        private void CopyBinaries(
            string hardwareFrameworkPath,
            string target,
            string binaryPath,
            string hashId)
        {
            var xclbinDirectoryPath = GetXclbinDirectoryPath(hardwareFrameworkPath, hashId);

            var binaryDirectoryPath = Path.GetDirectoryName(binaryPath);
            if (binaryDirectoryPath != null && !Directory.Exists(binaryDirectoryPath))
            {
                Directory.CreateDirectory(binaryDirectoryPath);
            }

            var builtFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin");
            File.Copy(builtFilePath, binaryPath);
            File.Copy(builtFilePath + InfoFileExtension, binaryPath + InfoFileExtension);
            Progress!(this, $"Files copied to binary folder ({builtFilePath}).");
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void CollectReport(
            string hardwareFrameworkPath,
            IHardwareImplementationCompositionContext context,
            string hashId)
        {
            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            var reportPath = Path.Combine(rtlDirectoryPath, "_x", "reports", "link", "imp");
            if (!Directory.Exists(reportPath))
            {
                _logger.LogWarning("Report directory is missing!");
                return;
            }

            var hash = context.HardwareDescription.TransformationId;
            var reportSavePath = Path.Combine(hardwareFrameworkPath, "reports", hash);
            if (!Directory.Exists(reportSavePath)) Directory.CreateDirectory(reportSavePath);

            var reportFiles = Directory.GetFiles(reportPath, "*.rpt");
            foreach (var reportFile in reportFiles)
            {
                File.Copy(reportFile, Path.Combine(reportSavePath, Path.GetFileName(reportFile)));
            }
        }

        private void Cleanup(string hardwareFrameworkPath, string hashId, bool deleteSource = true)
        {
            var path = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            if (deleteSource)
            {
                Directory.Delete(path, recursive: true);
            }
            else
            {
                var directories = new DirectoryInfo(path)
                    .GetDirectories()
                    .Where(directory => directory.Name != "src");
                foreach (var directory in directories) directory.Delete();
            }

            Progress!(this, "Build directory cleaned up.");
        }


        private void OnProgress(object sender, string message) =>
            _logger.LogInformation("Build step {0}/{1} completed: {2}", ++Step, StepsTotal, message);

        private Task ExecuteWithLogging(string executable, IList<string> arguments, string workingDirectory = null)
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

            var hasWorkingDirectory = Directory.Exists(workingDirectory);
            Command Configure(Command command)
            {
                if (hasWorkingDirectory) command = command.WithWorkingDirectory(workingDirectory!);
                return command.WithValidation(CommandResultValidation.None);
            }


            _logger.LogInformation(
                "Starting program: {0} {1} (working directory: {2})",
                executable,
                string.Join(" ", arguments),
                hasWorkingDirectory ? workingDirectory : ".");
            return CliHelper.StreamAsync(executable, arguments, OnCommandEvent, Configure);
        }

        private static string GetRtlDirectoryPath(string hardwareFrameworkPath, string hashId) =>
            Path.Combine(hardwareFrameworkPath, "rtl", hashId);

        private static string GetXclbinDirectoryPath(string hardwareFrameworkPath, string hashId) =>
            Path.Combine(GetRtlDirectoryPath(hardwareFrameworkPath, hashId), "xclbin");

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
