using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Helpers;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;
using static Hast.Vitis.Abstractions.Constants.Extensions;
using static System.Globalization.CultureInfo;

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

        private readonly ILogger _logger;
        private readonly string _buildOutputPath;
        private readonly StreamWriter _buildOutput;

        public event EventHandler<BuildProgressEventArgs> Progress;

        public Dictionary<string, BuildProviderShortcut> Shortcuts { get; } = new Dictionary<string, BuildProviderShortcut>();

        public int MajorStepsTotal { get; private set; } = 8;
        public int MajorStep { get; private set; }


        public VitisHardwareImplementationComposerBuildProvider(
            ILogger<VitisHardwareImplementationComposerBuildProvider> logger)
        {
            Progress += OnProgress;
            _logger = logger;

            var buildOutputPath = EnsureDirectoryExists("App_Data", "logs");
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


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest.ToolChainName == CommonToolChainNames.Vitis;

        public Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            if (!(context.DeviceManifest is XilinxDeviceManifest deviceManifest))
            {
                throw new InvalidOperationException(
                    $"The device manifest must be {nameof(XilinxDeviceManifest)} for " +
                    $"{nameof(VitisHardwareImplementationComposerBuildProvider)} to work.");
            }

            if (deviceManifest.SupportedPlatforms?.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The device manifest for '{deviceManifest.Name}' doesn't have any " +
                    $"{nameof(XilinxDeviceManifest.SupportedPlatforms)} which is required to build.");
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

            return BuildInnerAsync(context, implementation, deviceManifest, xilinxDirectoryPath);
        }

        private async Task BuildInnerAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation,
            XilinxDeviceManifest deviceManifest,
            string xilinxDirectoryPath)
        {
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
            _logger.LogInformation("HASH ID: {0}", hashId);
            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            implementation.BinaryPath = GetBinaryPath(context.Configuration, context.HardwareDescription);
            Cleanup(hardwareFrameworkPath, hashId);

            // If the xclbin exists then we are done here.
            if (File.Exists(implementation.BinaryPath))
            {
                ProgressMajor("A suitable XCLBIN is ready, no new build necessary.");

                if (!File.Exists(implementation.BinaryPath + InfoFileExtension))
                {
                    _logger.LogInformation(
                        "The info file (\"{0}\") does not exist. You may generate it using `xclbinutil --info`.",
                        implementation.BinaryPath + InfoFileExtension);
                }

                return;
            }

            _logger.LogInformation(
                "The xclbin file (\"{0}\") does not exist. Starting build...",
                implementation.BinaryPath);

            // Copy templates from ./HardwareFramework/rtl/src to the execution specific directory.
            var ipDirectoryPath = Path.Combine(GetRtlDirectoryPath(hardwareFrameworkPath, hashId), "src", "IP");
            if (!Directory.Exists(ipDirectoryPath))
            {
                FileSystem.CopyDirectory(
                    Path.Combine(hardwareFrameworkPath, "rtl", "src", "IP"),
                    ipDirectoryPath);
            }

            await ApplyTemplatesAsync(hardwareFrameworkPath, hashId, openClConfiguration);

            var platformsDirectories = (new[]
                {
                    Environment.GetEnvironmentVariable("XILINX_PLATFORM"),
                    Path.Combine(xilinxDirectoryPath!, "platforms"),
                    Path.Combine(hardwareFrameworkPath, "platforms"),
                })
                .Where(path => path != null && Directory.Exists(path))
                .Select(path => new DirectoryInfo(path))
                .ToList();

            // Using the variable names in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw_emu" : "hw";
            // Instead of the platform name like xilinx_u200_xdma_201830_2, you can use the full path of the .xpfm file
            // in the platform directory. This way you can override the platform directory by setting $XILINX_PLATFORM.
            // See: https://github.com/Xilinx/Vitis-Tutorials/issues/3.
            // We are looking for platform directories first, then xpfm files. Then by SupportedPlatforms and location:
            // 1. $XILINX_PLATFORM,
            // 2. /opt/xilinx/platforms
            // 3. ./HardwareFramework/platforms
            var device = deviceManifest.SupportedPlatforms!
                .SelectMany(platformName => platformsDirectories
                    .SelectMany(directoryInfo => directoryInfo
                        .GetDirectories($"{platformName}*")
                        .SelectMany(directory => directory.GetFiles("*.xpfm"))
                        .OrderByDescending(fileInfo => fileInfo.FullName))
                    .Union(platformsDirectories
                        .SelectMany(directoryInfo => directoryInfo.GetFiles($"{platformName}*.xpfm"))
                        .OrderByDescending(fileInfo => fileInfo.FullName))
                )
                .FirstOrDefault()?
                .FullName;

            if (device == null)
            {
                throw new FileNotFoundException(
                    "Unable to find the platform xpfm file. The supported platforms are: " +
                    string.Join(", ", deviceManifest.SupportedPlatforms));
            }

            var xclbinDirectoryPath = EnsureDirectoryExists(
                GetRtlDirectoryPath(hardwareFrameworkPath, hashId),
                "xclbin");

            await CreateSourceFilesAsync(context, hardwareFrameworkPath, hashId);

            // If the xclbin exists then we are done here.
            if (AllExist(implementation.BinaryPath, implementation.BinaryPath + ".info"))
            {
                ProgressMajor("A suitable XCLBIN is ready, no new build necessary.");
                return;
            }

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
            await BuildKernelAsync(
                hardwareFrameworkPath,
                target,
                device,
                hashId,
                deviceManifest,
                xclbinDirectoryPath,
                openClConfiguration);

            var disableHbm = deviceManifest.SupportsHbm && !openClConfiguration.UseHbm;
            CopyBinaries(target, implementation.BinaryPath, hashId, disableHbm);

            if (deviceManifest.RequiresDcpBinary)
            {
                ProgressMajor("There are no reports when then the project is compiled as netlist.");
            }
            else
            {
                ProgressMajor("Collecting reports.");
                try { await CollectReportsAsync(hardwareFrameworkPath, context, implementation, hashId); }
                catch (Exception e) { _logger.LogError(e, "Failed to collect reports."); }
            }

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
            XilinxDeviceManifest deviceManifest,
            string xclbinDirectoryPath,
            IOpenClConfiguration openClConfiguration)
        {
            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            var tmpDirectoryPath = EnsureDirectoryExists(GetTmpDirectoryPath(hashId));

            var xoFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xo");

            // vivado -mode batch -source $(GEN_XO_TLC) -tclargs $(XCLBIN)/hastip.$(TARGET).xo $(TARGET) $(DEVICE)
            //        $(PATH_TO_HDL) $(KERNEL_TCL) $(KERNEL_XML)
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
                Path.Combine(rtlDirectoryPath, "src", "IP"),
                Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", "package_kernel.tcl"),
                Path.Combine(rtlDirectoryPath, "src", "xml", "kernel.xml"),
            };
            await ExecuteWithLogging(vivadoExecutable, vivadoArguments, tmpDirectoryPath);
            ProgressMajor("Vivado build is finished.");


            // ifeq ($(MEMTYPE),$(filter $(MEMTYPE),DDR HBM PLRAM))
            //     CLFLAGS += --connectivity.sp hastip_1.buffer:$(MEMTYPE)[0:0]
            // endif
            // CLFLAGS += -g -R2 --save-temps -t $(TARGET) --platform $(DEVICE) --dk chipscope:hastip_1:m_axi_gmem
            // v++ $(CLFLAGS) --kernel_frequency $(FREQUENCY) -lo $(XCLBIN)/hastip.$(TARGET).xclbin $(XO_FILE)
            var vppExecutable = (await GetExecutablePathAsync(Vpp));
            var vppArguments = new List<string>(
                deviceManifest.SupportsHbm && openClConfiguration.UseHbm
                ? new[] { "--connectivity.sp", "hastip_1.buffer:HBM[0:0]" }
                : Array.Empty<string>());

            if (deviceManifest.RequiresDcpBinary)
            {
                vppArguments.Add("--advanced.param");
                vppArguments.Add("compiler.acceleratorBinaryContent=dcp");
            }

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
                (deviceManifest.ClockFrequencyHz / 1_000_000).ToString(InvariantCulture),
                "-lo",
                Path.Combine(tmpDirectoryPath, $"hastip.{target}.xclbin"),
                xoFilePath,
            });

            await ExecuteWithLogging(vppExecutable, vppArguments, tmpDirectoryPath);
            ProgressMajor("v++ build is finished.");

            if (target.ToUpperInvariant() == "HW_EMU")
            {
                // For example:
                // emconfigutil --platform xilinx_u200_xdma_201830_2 --od ./HardwareFramework/rtl/xclbin/
                var emConfigExecutable = (await GetExecutablePathAsync("emconfigutil"));
                var emConfigArguments = new[] { "--platform", device, "--od", tmpDirectoryPath, };
                await ExecuteWithLogging(emConfigExecutable, emConfigArguments, rtlDirectoryPath);
                File.Copy(Path.Combine(tmpDirectoryPath, "emconfig.json"), "emconfig.json");
                ProgressMajor("Emulation configuration (emconfig) setup is finished.");
            }
        }

        private async Task SynthKernelAsync(string hardwareFrameworkPath, string hashId)
        {
            ProgressMajor("Starting Vivado synthesis.");

            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);

            // vivado -mode batch -source synth_util.tcl $(VhdFileIn) $(RptFileOut)
            var vivadoExecutable = (await GetExecutablePathAsync("vivado"));
            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", "synth_util.tcl"),
                "-tclargs",
                Path.Combine(rtlDirectoryPath, "src", "IP", "Hast_IP.vhd"),
                Path.Combine(EnsureDirectoryExists(hardwareFrameworkPath, "reports", hashId), "Hast_IP_synth_util.rpt"),
            };
            await ExecuteWithLogging(vivadoExecutable, vivadoArguments);
            ProgressMajor("Vivado synthesis is finished.");
        }

        private void CopyBinaries(
            string target,
            string binaryPath,
            string hashId,
            bool disableHbm)
        {
            var binaryDirectoryPath = Path.GetDirectoryName(binaryPath);
            if (binaryDirectoryPath != null) EnsureDirectoryExists(binaryDirectoryPath);

            var builtFilePath = Path.Combine(GetTmpDirectoryPath(hashId), $"hastip.{target}.xclbin");
            File.Copy(builtFilePath, binaryPath);
            File.Copy(builtFilePath + InfoFileExtension, binaryPath + InfoFileExtension);
            if (disableHbm) File.Create(binaryPath + NoHbmFlagExtension).Dispose();
            ProgressMajor($"Files copied to binary folder ({builtFilePath}).");
        }

        private async Task CollectReportsAsync(
            string hardwareFrameworkPath,
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation,
            string hashId)
        {
            var reportPath = Path.Combine(GetTmpDirectoryPath(hashId), "_x", "reports", "link", "imp");
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
            var report = await XilinxReport.ParseAsync(reader);

            var jsonFilePath = Path.Combine(reportSavePath, "report.json");
            await File.WriteAllTextAsync(jsonFilePath, JsonConvert.SerializeObject(report, Formatting.Indented));
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
                    InvariantCulture,
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
                        "means that it has used some other resource type to not go above 100% (eg. LUT instead of " +
                        "DSP) which results in performance degradation and potential loss of accuracy. " +
                        TryToMakeItSmaller);
                }

                implementation.ResourceUsagePercent[resourceType] = utilization;
            }

            try
            {
                implementation.PowerUsageWatts = decimal.Parse(
                    report.Sections["1. Summary"].ToDictionaryByFirstColumn()["Total On-Chip Power (W)"][Value],
                    InvariantCulture);
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

        private void Cleanup(string hardwareFrameworkPath, string hashId)
        {
            var rtlDirectory = new DirectoryInfo(GetRtlDirectoryPath(hardwareFrameworkPath, hashId));
            if (rtlDirectory.Exists)
            {
                try
                {
                    foreach (var file in rtlDirectory.GetFiles()) file.Delete();
                    foreach (var subDirectory in rtlDirectory.GetDirectories().Where(sub => sub.Name != "src"))
                    {
                        subDirectory.Delete(recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up.");
                }
            }

            var tmpDirectory = new DirectoryInfo(GetTmpDirectoryPath(hashId));
            if (tmpDirectory.Exists) tmpDirectory.Delete(recursive: true);

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
                        Log(
                            LogLevel.None,
                            name,
                            $"#{started.ProcessId} arguments:\n\t{string.Join("\n\t", arguments)}",
                            "started");
                        break;
                    case StandardOutputCommandEvent output:
                        Log(LogLevel.Trace, name, output.Text, "stdout");
                        break;
                    case StandardErrorCommandEvent error:
                        Log(LogLevel.Warning, name, error.Text, "stderr");
                        break;
                    case ExitedCommandEvent exited:
                        var message = (exited.ExitCode == 0 ? "success" : "failure") + "\n\n\n";
                        Log(LogLevel.Information, name, message, "finished");

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

            // Find informational messages and escalate their log level since most of them will be "trace" by default.
            if (text?.Contains(':') == true)
            {
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
                    "The linker encountered an error. This is typically because the resulting hardware design won't " +
                    "fit on the FPGA as it's too complex. Try to make your code simpler (make it shorter, use " +
                    "smaller data types and a lower degree of parallelism) until this error goes away.");
            }

            _logger.Log(logLevel, "{0}: {1}", name, message);
            _buildOutput.WriteLine("{0} {2}: {1}", name, message, buildLogType);
        }


        private static Task<bool> CreateSourceFilesAsync(
            IHardwareImplementationCompositionContext context,
            string hardwareFrameworkPath,
            string hashId)
        {
            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            var ipDirectoryPath = EnsureDirectoryExists(rtlDirectoryPath, "src", "IP");
            var vhdlFilePath = Path.Combine(ipDirectoryPath, "Hast_IP.vhd");
            var xdcFilePath = Path.Combine(ipDirectoryPath, "Hast_IP.xdc");

            return VhdlHelper.CreateVhdlAndXdcFilesAsync(context, xdcFilePath, vhdlFilePath);
        }

        private static string GetRtlDirectoryPath(string hardwareFrameworkPath, string hashId) =>
            Path.Combine(hardwareFrameworkPath, "rtl", hashId);

        private static string GetTmpDirectoryPath(string hashId) =>
            Path.Combine(Path.DirectorySeparatorChar.ToString(), "tmp", "hastlayer", hashId);

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

        private static async Task ApplyTemplatesAsync(
            string hardwareFrameworkPath,
            string hashId,
            IOpenClConfiguration openClConfiguration)
        {
            var sourceDirectoryPath = Path.Combine(hardwareFrameworkPath, "rtl", "src");
            var targetDirectoryPath = Path.Combine(hardwareFrameworkPath, "rtl", hashId, "src");
            var files = new[]
            {
                Path.Combine("xml", "kernel.xml"),
                Path.Combine("IP", "hastip.v"),
                Path.Combine("testbench", "slv_m00_axi_vip_pkg.sv"),
                Path.Combine("testbench", "slv_m00_axi_vip.sv"),
                Path.Combine("testbench", "hastip_tb.sv"),
            };

            foreach (var file in files)
            {
                var result = (await File.ReadAllTextAsync(Path.Combine(sourceDirectoryPath, file) + ".template"))
                    .Replace("###hastipAxiDWidth###", openClConfiguration.AxiBusWith.ToString(InvariantCulture))
                    .Replace("###hastipCache###", openClConfiguration.UseCache ? "1" : "0");
                var targetFilePath = Path.Combine(targetDirectoryPath, file);
                EnsureDirectoryExists(Path.GetDirectoryName(targetFilePath));
                await File.WriteAllTextAsync(targetFilePath, result);
            }

        }

        public void Dispose() => _buildOutput?.Dispose();

        public static string GetBinaryPath(
            IHardwareGenerationConfiguration configuration,
            IHardwareDescription hardwareDescription)
        {
            var hashId = hardwareDescription.TransformationId;
            var hardwareFrameworkPath = Path.GetFullPath(configuration.HardwareFrameworkPath);
            return Path.Combine(
                EnsureDirectoryExists(hardwareFrameworkPath, "bin"),
                hashId + ".xclbin");
        }
    }
}
