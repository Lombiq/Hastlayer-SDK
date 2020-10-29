using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private const string Value = XilinxReportSection.Value;
        private const string Vpp = "v++";
        private const string UtilizationPercent = "Utilization (%)";
        private const string TryToMakeItSmaller = "Try to make your code simpler (make it shorter, use smaller data " +
                                                  "types, use a lower degree of parallelism) until it goes below 80%.";

        private static bool _firstRun = true;
        private static readonly string[] _vppStatusLogs = { "] Starting ", "] Phase ", "] Finished " };

        private const string InfoFileExtension = OpenClCommunicationService.InfoFileExtension;

        private readonly ILogger _logger;
        private readonly string _buildOutputPath;
        private readonly StreamWriter _buildOutput;

        public event EventHandler<BuildProgressEventArgs> Progress;

        public int MajorStepsTotal { get; private set; } = 8;
        public int MajorStep { get; private set; }


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
                _buildOutputPath = Path.Combine(buildOutputPath, fileName);

                try
                {
                    _buildOutput = new StreamWriter(_buildOutputPath, append: false, Encoding.UTF8);
                }
                catch (IOException ex)
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
                throw new InvalidOperationException(
                    $"The device manifest must be {nameof(XilinxDeviceManifest)} for " +
                    $"{nameof(VitisHardwareImplementationComposerBuildProvider)} to work.");
            }

            if (string.IsNullOrEmpty(deviceManifest.TechnicalName))
            {
                throw new InvalidOperationException(
                    $"The device manifest for '{deviceManifest.Name}' is missing its technical name which is required to build.");
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XILINX_VITIS")))
            {
                throw new InvalidOperationException(
                    "XILINX_VITIS variable is not set. This is required to run Vivado. For further instructions see " +
                    "https://www.xilinx.com/html_docs/xilinx2020_1/vitis_doc/settingupvitisenvironment.html");
            }

            var xilinxDirectoryPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("XILINX_XRT"));
            if (!Directory.Exists(xilinxDirectoryPath))
            {
                throw new InvalidOperationException(
                    "XILINX_XRT variable is not set or it is not pointing to an existing directory.");
            }

            var buildConfiguration = context.Configuration.GetOrAddVitisBuildConfiguration();
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();

            if (buildConfiguration.SynthesisOnly)
            {
                MajorStepsTotal = 3;
            }
            // Synthesis doesn't need the device.
            else if (buildConfiguration.ResetOnFirstRun && _firstRun)
            {
                _firstRun = false;
                await EnsureDeviceReady();
            }

            ProgressMajor(
                "Environment is ready, starting build. Simpler algorithms take 2-3 hours to compile, more complex " +
                "ones usually up to 4. Although 15 hours are also possible if the hardware is completely utilized " +
                "with extremely complex and/or very highly parallelized algorithms.");

            var hashId = context.HardwareDescription.TransformationId;
            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
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

            if (buildConfiguration.SynthesisOnly)
            {
                await SynthKernelAsync(hardwareFrameworkPath, hashId);

                if (context.Configuration is HardwareGenerationConfiguration configuration)
                {
                    configuration.EnableHardwareImplementationComposition = false;
                }

                return;
            }

            ProgressMajor("Staring build.");
            await BuildKernelAsync(hardwareFrameworkPath, target, device, hashId, deviceManifest);
            CopyBinaries(hardwareFrameworkPath, target, implementation.BinaryPath, hashId);

            ProgressMajor("Collecting reports.");
            try { CollectReports(hardwareFrameworkPath, context, implementation, hashId); }
            catch (Exception e) { _logger.LogError(e, "Failed to collect reports."); }

            Cleanup(hardwareFrameworkPath, hashId);
        }

        private void ProgressMajor(string message)
        {
            MajorStep++;
            Progress?.Invoke(this, new BuildProgressEventArgs(message, isMajorStep: true));
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

            // vivado -mode batch -source $(GEN_XO_TLC) -tclargs $(XCLBIN)/hastip.$(TARGET).xo $(TARGET) $(DEVICE)
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
            ProgressMajor("Vivado build is finished.");


            // ifeq ($(MEMTYPE),$(filter $(MEMTYPE),DDR HBM PLRAM))
            //     CLFLAGS += --connectivity.sp hastip_1.buffer:$(MEMTYPE)[0:0]
            // endif
            // CLFLAGS += -g -R2 --save-temps -t $(TARGET) --platform $(DEVICE) --dk chipscope:hastip_1:m_axi_gmem
            // v++ $(CLFLAGS) --kernel_frequency $(FREQUENCY) -lo $(XCLBIN)/hastip.$(TARGET).xclbin $(XO_FILE)
            var vppExecutable = (await GetExecutablePathAsync(Vpp));
            var vppArguments = new List<string>(
                deviceManifest.SupportsHbm
                ? new[] { "--connectivity.sp", "hastip_1.buffer:HBM[0:0]" }
                : Array.Empty<string>());
            vppArguments.AddRange(new[]
            {
                "-g",
                "-R2",
                "--save-temps",
                "-t",
                target,
                "--platform",
                device,
                "--dk",
                "chipscope:hastip_1:m_axi_gmem",
                "--kernel_frequency",
                (deviceManifest.ClockFrequencyHz / 1_000_000).ToString(CultureInfo.InvariantCulture),
                "-lo",
                Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xclbin"),
                xoFilePath,
            });

            await ExecuteWithLogging(vppExecutable, vppArguments, rtlDirectoryPath);
            ProgressMajor("v++ build is finished.");

            if (target.ToUpperInvariant() == "HW_EMU")
            {
                // For example:
                // emconfigutil --platform xilinx_u200_xdma_201830_2 --od ./HardwareFramework/rtl/xclbin/
                var emConfigExecutable = (await GetExecutablePathAsync("emconfigutil"));
                var emConfigArguments = new[] { "--platform", device, "--od", xclbinDirectoryPath, };
                await ExecuteWithLogging(emConfigExecutable, emConfigArguments, rtlDirectoryPath);
                File.Copy(Path.Combine(xclbinDirectoryPath, "emconfig.json"), "emconfig.json");
                ProgressMajor("Emulation configuration (emconfig) setup is finished.");
            }
        }

        private async Task SynthKernelAsync(string hardwareFrameworkPath, string hashId)
        {
            ProgressMajor("Starting Vivado synthesis.");

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
            ProgressMajor("Vivado synthesis is finished.");
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
            ProgressMajor($"Files copied to binary folder ({builtFilePath}).");
        }

        private void CollectReports(
            string hardwareFrameworkPath,
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation,
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

            var reportFilePath = Directory.GetFiles(reportSavePath, "*_bb_locked_power_routed.rpt").FirstOrDefault();
            if (reportFilePath == null)
            {
                throw new FileNotFoundException(
                    "The report file is missing. Utilization related verification is not performed.");
            }

            using var reader = File.OpenText(reportFilePath);
            var report = XilinxReport.Parse(reader);

            var jsonFilePath = Path.Combine(reportSavePath, "report.json");
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(report, Formatting.Indented));
            _logger.LogInformation("A converted JSON file is saved to {0}.", jsonFilePath);

            var componentsSection = report.Sections["1.1 On-Chip Components"].ToDictionaryByFirstColumn();
            _logger.LogInformation(
                "On-Chip Components: {0}",
                JsonConvert.SerializeObject(componentsSection, Formatting.Indented));
            foreach (var (resourceType, row) in componentsSection)
            {
                if (!decimal.TryParse(
                    row[UtilizationPercent],
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var utilization))
                {
                    continue;
                }

                if (resourceType.ToUpperInvariant().Contains("LUT AS LOGIC"))
                {
                    CheckLutUtilization(utilization);
                }
                else if (utilization > 99.0m)
                {
                    throw new InvalidOperationException(
                        $"The resulting hardware design is completely utilizing '{resourceType}' which likely also " +
                        $"means that it has used some other resource type to not go above 100% (eg. LUT instead of " +
                        $"DSP) which results in performance degradation and potential loss of accuracy. " +
                        TryToMakeItSmaller);
                }

                implementation.ResourceUsagePercent[resourceType] = utilization;
            }

            try
            {
                implementation.PowerUsageWatts = decimal.Parse(
                    report.Sections["1. Summary"].ToDictionaryByFirstColumn()["Total On-Chip Power (W)"][Value],
                    CultureInfo.InvariantCulture);
                _logger.LogInformation("Total on-chip power: {0}W", implementation.PowerUsageWatts);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to acquire hardware wattage information.", ex);
            }
        }

        private void CheckLutUtilization(decimal lutUtilization)
        {
            if (lutUtilization > 90.0m)
            {
                throw new InvalidOperationException(
                    "The resulting hardware design is using more than 90% of the FPGA's resources. This will " +
                    "almost always be unstable and randomly give incorrect results. " + TryToMakeItSmaller);
            }

            if (lutUtilization > 80.0m)
            {
                _logger.LogWarning(
                    "The resulting hardware design is using more than 80% of the FPGA's resources. While this " +
                    "may work fine the result can be potentially unstable and randomly give incorrect results. " +
                    TryToMakeItSmaller);
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

            ProgressMajor("Build directory cleaned up.");
        }

        private async Task EnsureDeviceReady()
        {
            _logger.LogWarning(
                "This is the first build with the current process. Resetting the devices for a clean state...");

            var yes = PipeSource.FromString("y" + Environment.NewLine);
            var xbutil = Cli.Wrap((await CliHelper.WhichAsync("xbutil")).First().FullName)
                .WithArguments(new[] { "reset" })
                .WithValidation(CommandResultValidation.None);
            var result = await (yes | xbutil).ExecuteBufferedAsync();

            _logger.LogWarning("xbutil: {0}", result.StandardOutput);
            _buildOutput.WriteLine("xbutil stdout: {0}", result.StandardOutput);
        }

        private void OnProgress(object sender, BuildProgressEventArgs e) =>
            _logger.LogInformation(
                "Message on build step {0}/{1}{3}: {2}",
                MajorStep,
                MajorStepsTotal,
                e.Message,
                e.IsMajorStep ? " (new)" : string.Empty);

        private Task ExecuteWithLogging(string executable, IList<string> arguments, string workingDirectory = null)
        {
            var name = Path.GetFileName(executable);
            void OnCommandEvent(CommandEvent commandEvent)
            {
                switch (commandEvent)
                {
                    case StartedCommandEvent started:
                        Log(LogLevel.Information, name, started.ProcessId, "started");
                        break;
                    case StandardOutputCommandEvent output:
                        Log(LogLevel.Trace, name, output.Text, "stdout");
                        break;
                    case StandardErrorCommandEvent error:
                        Log(LogLevel.Warning, name, error.Text, "stderr");
                        break;
                    case ExitedCommandEvent exited:
                        Log(LogLevel.Information, name, exited.ExitCode == 0 ? "success" : "failure", "finished");

                        if (exited.ExitCode != 0)
                        {
                            throw new CommandExecutionException(
                                $"The command {name} exited with code {exited.ExitCode}. " +
                                $"You can review the output at '{Path.GetFullPath(_buildOutputPath)}'.");
                        }
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

        private void Log(LogLevel logLevel, string name, object message, string buildLogType)
        {
            var text = message as string;
            if (text?.Contains(':') == true)
            {
                // Find informational messages and escalate their log level since most of them will be "trace" by default.
                logLevel = text.Split(':')[0].Trim().ToUpperInvariant() switch
                {
                    "ERROR" => LogLevel.Error,
                    "CRITICAL WARNING" => LogLevel.Warning,
                    "WARNING" => LogLevel.Warning,
                    "INFO" when logLevel < LogLevel.Information => LogLevel.Information,
                    _ => logLevel,
                };
            }

            // Raise the v++ status outputs like "[21:17:26] Phase 1 Build RT Design" trough the Progress event.
            if (name == Vpp && text?.StartsWith("[") == true && _vppStatusLogs.Any(fragment => text.Contains(fragment)))
            {
                if (logLevel < LogLevel.Information) logLevel = LogLevel.Information;
                Progress?.Invoke(this, new BuildProgressEventArgs(text));
            }

            if (logLevel == LogLevel.Error && text?.Contains("Failed to finish platform linker") == true)
            {
                throw new InvalidOperationException(
                    "The linker encountered an error. Typically because the resulting hardware design won't fit on " +
                    "the FPGA as it's too complex. Try to make your code simpler (make it shorter, use smaller data " +
                    "types, use a lower degree of parallelism) until this error goes away.");
            }

            _logger.Log(logLevel, "{0}: {1}", name, message);
            _buildOutput.WriteLine("{0} {2}: {1}\n\n\n", name, message, buildLogType);
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
