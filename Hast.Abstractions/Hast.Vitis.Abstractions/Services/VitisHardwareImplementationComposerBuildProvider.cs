using CliWrap;
using CliWrap.Buffered;
using Hast.Common.Enums;
using Hast.Common.Helpers;
using Hast.Common.Interfaces;
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
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;
using static Hast.Vitis.Abstractions.Constants.Extensions;

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

        private readonly ILogger<VitisHardwareImplementationComposerBuildProvider> _logger;
        private readonly IHastlayerFlavorProvider _flavorProvider;
        private readonly BuildLogger<VitisHardwareImplementationComposerBuildProvider> _buildLogger;
        private readonly TextWriter _buildOutput;

        private static bool _firstRun = true;

        public event EventHandler<BuildProgressEventArgs> Progress;

        public IDictionary<string, BuildProviderShortcut> Shortcuts { get; } =
            new Dictionary<string, BuildProviderShortcut>();

        public int MajorStepsTotal { get; private set; }
        public int MajorStep { get; private set; }

        public VitisHardwareImplementationComposerBuildProvider(
            ILogger<VitisHardwareImplementationComposerBuildProvider> logger,
            IHastlayerFlavorProvider flavorProvider)
        {
            Progress += OnProgress;
            _logger = logger;
            _flavorProvider = flavorProvider;

            (_buildLogger, _buildOutput) = BuildLogger.Create(logger, this);
        }

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is VitisDeviceManifest;

        public Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            if (context.DeviceManifest is not VitisDeviceManifest deviceManifest)
            {
                throw new InvalidOperationException(
                    $"The device manifest must be {nameof(VitisDeviceManifest)} for " +
                    $"{nameof(VitisHardwareImplementationComposerBuildProvider)} to work.");
            }

            if (deviceManifest.SupportedPlatforms?.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The device manifest for '{deviceManifest.Name}' doesn't have any " +
                    $"{nameof(VitisDeviceManifest.SupportedPlatforms)} which is required to build.");
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XILINX_VITIS")))
            {
                // When cross-compiling, the build machine needs Vivado and XRT, but the FPGA machine only needs XRT.
                _logger.LogWarning(
                    "XILINX_VITIS variable is not set. This is required to build using Vivado. For further instructions " +
                    "see https://www.xilinx.com/html_docs/xilinx2020_1/vitis_doc/settingupvitisenvironment.html.");
            }

            var xilinxDirectoryPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("XILINX_XRT"));
            if (!Directory.Exists(xilinxDirectoryPath))
            {
                throw new InvalidOperationException(
                    "XILINX_XRT variable is not set or it is not pointing to an existing directory.");
            }

            return BuildInnerAsync(context, implementation, deviceManifest);
        }

        private async Task BuildInnerAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation,
            VitisDeviceManifest deviceManifest)
        {
            var buildConfiguration = context.Configuration.GetOrAddVitisBuildConfiguration();
            var openClConfiguration = context.Configuration.GetOrAddOpenClConfiguration();

            var hashId = context.HardwareDescription.TransformationId;
            _logger.LogInformation("HASH ID: {0}", hashId);

            MajorStepsTotal = buildConfiguration.SynthesisOnly ? 3 : 8;

            // Synthesis doesn't need the device.
            await EnsureDeviceReadyAsync(buildConfiguration);

            ProgressMajor(
                "Environment is ready, starting build. Simpler algorithms take 2-3 hours to compile, more complex " +
                "ones usually up to 4. Although 15 hours are also possible if the hardware is completely utilized " +
                "with extremely complex and/or very highly parallelized algorithms.");

            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            implementation.BinaryPath = GetBinaryPath(context.Configuration, context.HardwareDescription);
            Cleanup(hardwareFrameworkPath, hashId);

            var promptBeforeBuild =
                buildConfiguration.PromptBeforeBuild &&
                _flavorProvider.Flavor != HastlayerFlavor.Client;
            await CreateSourceAsync(
                context,
                hardwareFrameworkPath,
                hashId,
                implementation.BinaryPath,
                promptBeforeBuild);

            if (CheckIfDoneAlready(implementation)) return;

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

            await ApplyTemplatesAsync(hardwareFrameworkPath, hashId, openClConfiguration, deviceManifest);

            var xilinxDirectoryPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("XILINX_XRT"));
            if (!Directory.Exists(xilinxDirectoryPath))
            {
                throw new InvalidOperationException(
                    "XILINX_XRT variable is not set or it is not pointing to an existing directory.");
            }

            var platformsDirectories = new[]
                {
                    Environment.GetEnvironmentVariable("XILINX_PLATFORM"),
                    Path.Combine(xilinxDirectoryPath!, "platforms"),
                    Path.Combine(hardwareFrameworkPath, "platforms"),
                }
                .Where(path => path != null && Directory.Exists(path))
                .Select(path => new DirectoryInfo(path))
                .ToList();
            var device = GetPlatformFilePath(deviceManifest, platformsDirectories);

            // Using the variable names in the Makefile.
            var target = openClConfiguration.UseEmulation ? "hw_emu" : "hw";

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
                context,
                target,
                device,
                hashId,
                deviceManifest,
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

        private static string GetPlatformFilePath(
            VitisDeviceManifest deviceManifest,
            List<DirectoryInfo> platformsDirectories)
        {
            // Instead of the platform name like xilinx_u200_xdma_201830_2, you can use the full path of the .xpfm file
            // in the platform directory. This way you can override the platform directory by setting $XILINX_PLATFORM.
            // See: https://github.com/Xilinx/Vitis-Tutorials/issues/3.
            // We are looking for platform directories first, then xpfm files. Then by SupportedPlatforms and location:
            // 1. $XILINX_PLATFORM,
            // 2. /opt/xilinx/platforms
            // 3. ./HardwareFramework/platforms
            var caseInsensitiveEnumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MatchCasing = MatchCasing.CaseInsensitive,
            };
            var device = deviceManifest.SupportedPlatforms!
                .SelectMany(platformName => platformsDirectories
                    .SelectMany(directoryInfo => directoryInfo
                        .GetDirectories($"{platformName}*", caseInsensitiveEnumerationOptions)
                        .SelectMany(directory => directory.GetFiles("*.xpfm", caseInsensitiveEnumerationOptions))
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

            return device;
        }

        private void ProgressMajor(string message)
        {
            MajorStep++;
            InvokeProgress(new BuildProgressEventArgs(message, isMajorStep: true));
        }

        private async Task BuildKernelAsync(
            IHardwareImplementationCompositionContext context,
            string target,
            string device,
            string hashId,
            VitisDeviceManifest deviceManifest,
            IOpenClConfiguration openClConfiguration)
        {
            var (hardwareFrameworkPath, tmpDirectoryPath, xclbinFilePath, xclbinDirectoryPath) = GetBuildPaths(context);

            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            var xoFilePath = Path.Combine(xclbinDirectoryPath, $"hastip.{target}.xo");

            // vivado -mode batch -source $(GEN_XO_TLC) -tclargs $(XCLBIN)/hastip.$(TARGET).xo $(TARGET) $(DEVICE)
            //        $(PATH_TO_HDL) $(KERNEL_TCL) $(KERNEL_XML)
            var vivadoExecutable = await GetExecutablePathAsync("vivado");
            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                GetScriptFile(hardwareFrameworkPath, "gen_xo.tcl"),
                "-tclargs",
                xoFilePath,
                target,
                device,
                Path.Combine(rtlDirectoryPath, "src", "IP"),
                GetScriptFile(hardwareFrameworkPath, "package_kernel.tcl"),
                Path.Combine(rtlDirectoryPath, "src", "xml", "kernel.xml"),
            };
            await _buildLogger.ExecuteWithLoggingAsync(vivadoExecutable, vivadoArguments, tmpDirectoryPath);
            ProgressMajor("Vivado build is finished.");

            // But this is not C# code.
#pragma warning disable S125 // Sections of code should not be commented out.
            // ifeq ($(MEMTYPE),$(filter $(MEMTYPE),DDR HBM PLRAM))
            //     CLFLAGS += --connectivity.sp hastip_1.buffer:$(MEMTYPE)[0:0]
            // endif
            // CLFLAGS += -g -R2 --save-temps -t $(TARGET) --platform $(DEVICE) \
            //            --advanced.param compiler.skipTimingCheckAndFrequencyScaling=1 --optimize 3
            // v++ $(CLFLAGS) -lo $(XCLBIN)/hastip.$(TARGET).xclbin $(XO_FILE)
#pragma warning restore S125 // Sections of code should not be commented out.
            var vppExecutable = await GetExecutablePathAsync(Vpp);
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
                "--advanced.param",
                "compiler.skipTimingCheckAndFrequencyScaling=1",
                "--optimize",
                "3",
            });

            if (deviceManifest.BuildWithClockFrequencyHz)
            {
                vppArguments.AddRange(new[]
                {
                    "--kernel_frequency",
                    (deviceManifest.ClockFrequencyHz / 1_000_000).ToString(CultureInfo.CurrentCulture),
                });
            }

            vppArguments.AddRange(new[]
            {
                "-lo",
                Path.Combine(tmpDirectoryPath, $"hastip.{target}.xclbin"),
                xclbinFilePath,
                xoFilePath,
            });

            await _buildLogger.ExecuteWithLoggingAsync(vppExecutable, vppArguments, tmpDirectoryPath);
            ProgressMajor("v++ build is finished.");

            if (target.ToUpperInvariant() == "HW_EMU")
            {
                // For example:
                // emconfigutil --platform xilinx_u200_xdma_201830_2 --od ./HardwareFramework/rtl/xclbin/
                var emConfigExecutable = await GetExecutablePathAsync("emconfigutil");
                var emConfigArguments = new[] { "--platform", device, "--od", tmpDirectoryPath, };
                await _buildLogger.ExecuteWithLoggingAsync(emConfigExecutable, emConfigArguments, rtlDirectoryPath);
                File.Copy(Path.Combine(tmpDirectoryPath, "emconfig.json"), "emconfig.json");
                ProgressMajor("Emulation configuration (emconfig) setup is finished.");
            }
        }

        private async Task SynthKernelAsync(string hardwareFrameworkPath, string hashId)
        {
            ProgressMajor("Starting Vivado synthesis.");

            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);

            // vivado -mode batch -source synth_util.tcl $(VhdFileIn) $(RptFileOut)
            var vivadoExecutable = await GetExecutablePathAsync("vivado");
            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                GetScriptFile(hardwareFrameworkPath, "synth_util.tcl"),
                "-tclargs",
                Path.Combine(rtlDirectoryPath, "src", "IP", "Hast_IP.vhd"),
                Path.Combine(EnsureDirectoryExists(hardwareFrameworkPath, "reports", hashId), "Hast_IP_synth_util.rpt"),
            };
            await _buildLogger.ExecuteWithLoggingAsync(vivadoExecutable, vivadoArguments);
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

            var reportFilePath =
                Directory.GetFiles(reportSavePath, "*_bb_locked_power_routed.rpt").FirstOrDefault() ??
                Directory.GetFiles(reportSavePath, "*_power_routed.rpt").FirstOrDefault();
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
                    CultureInfo.InvariantCulture,
                    out var utilization))
                {
                    continue;
                }

                if (resourceType.ContainsOrdinalIgnoreCase("LUT AS LOGIC"))
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
                    CultureInfo.InvariantCulture);
                _logger.LogInformation("Total on-chip power: {0}W", implementation.PowerUsageWatts);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to acquire hardware wattage information.", ex);
            }
        }

        private bool CheckIfDoneAlready(IHardwareImplementation implementation)
        {
            // If the xclbin exists then we are done here.
            var exists = File.Exists(implementation.BinaryPath);

            if (exists)
            {
                ProgressMajor("A suitable XCLBIN is ready, no new build necessary.");

                if (!File.Exists(implementation.BinaryPath + InfoFileExtension))
                {
                    _logger.LogInformation(
                        "The info file (\"{0}\") does not exist. You may generate it using `xclbinutil --info`.",
                        implementation.BinaryPath + InfoFileExtension);
                }
            }

            return exists;
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

        private async Task EnsureDeviceReadyAsync(VitisBuildConfiguration buildConfiguration)
        {
            if (buildConfiguration.SynthesisOnly || !buildConfiguration.ResetOnFirstRun || !_firstRun) return;

            _firstRun = false;

            _logger.LogWarning(
                "This is the first build with the current process. Resetting the devices for a clean state...");

            var yes = PipeSource.FromString("y" + Environment.NewLine);
            var xbutil = Cli.Wrap((await CliHelper.WhichAsync("xbutil")).First().FullName)
                .WithArguments(new[] { "reset" })
                .WithValidation(CommandResultValidation.None);
            var result = await (yes | xbutil).ExecuteBufferedAsync();

            _logger.LogWarning("xbutil: {0}", result.StandardOutput);
            await _buildOutput.WriteLineAsync($"xbutil stdout: {result.StandardOutput}");
        }

        private void OnProgress(object sender, BuildProgressEventArgs e) =>
            BuildLogger.OnProgress(_logger, MajorStep, MajorStepsTotal, e);

        private static async Task CreateSourceAsync(
            IHardwareImplementationCompositionContext context,
            string hardwareFrameworkPath,
            string hashId,
            string binaryPath,
            bool promptBeforeBuild)
        {
            var rtlDirectoryPath = GetRtlDirectoryPath(hardwareFrameworkPath, hashId);
            var ipDirectoryPath = EnsureDirectoryExists(rtlDirectoryPath, "src", "IP");
            var vhdlFilePath = Path.Combine(ipDirectoryPath, "Hast_IP.vhd");
            var xdcFilePath = Path.Combine(ipDirectoryPath, "Hast_IP.xdc");

            await VhdlHelper.CreateVhdlAndXdcFilesAsync(context, xdcFilePath, vhdlFilePath);

            if (promptBeforeBuild)
            {
                Console.WriteLine(
                    $"The source files have been written to:\n" +
                    $"    {vhdlFilePath}\n" +
                    $"    {xdcFilePath}\n" +
                    $"Expected result:\n" +
                    $"    {binaryPath}\n\n" +
                    $"Press [enter] when you are ready to continue.");
                Console.ReadLine();
            }
        }

        private static string GetRtlDirectoryPath(string hardwareFrameworkPath, string hashId) =>
            Path.Combine(hardwareFrameworkPath, "rtl", hashId);

        private static string GetTmpDirectoryPath(string hashId) =>
            Path.Combine(Path.DirectorySeparatorChar.ToString(), "tmp", "hastlayer", hashId);

        public static async Task<string> GetExecutablePathAsync(string executable)
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
            IOpenClConfiguration openClConfiguration,
            VitisDeviceManifest manifest)
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
                    .Replace("###hastipAxiDWidth###", manifest.AxiBusWith.ToTechnicalString())
                    .Replace("###hastipCache###", openClConfiguration.UseCache ? "1" : "0");
                var targetFilePath = Path.Combine(targetDirectoryPath, file);
                EnsureDirectoryExists(Path.GetDirectoryName(targetFilePath));
                await File.WriteAllTextAsync(targetFilePath, result);
            }

            // Also copy all IP files.
            foreach (var file in Directory.GetFiles(Path.Combine(sourceDirectoryPath, "IP")))
            {
                if (file.EndsWith(".template", StringComparison.OrdinalIgnoreCase)) continue;

                var targetFilePath = Path.Combine(targetDirectoryPath, "IP", Path.GetFileName(file));
                if (!File.Exists(targetFilePath)) File.Copy(file, targetFilePath);
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

        public static string GetScriptFile(string hardwareFrameworkPath, string fileName) =>
            Path.Combine(hardwareFrameworkPath, "rtl", "src", "scripts", fileName);

        public void InvokeProgress(BuildProgressEventArgs eventArgs) => Progress?.Invoke(this, eventArgs);

        public static (string HardwareFrameworkPath, string TmpDirectoryPath, string XclbinFilePath, string XclbinDirectoryPath)
            GetBuildPaths(IHardwareImplementationCompositionContext context)
        {
            var hashId = context.HardwareDescription.TransformationId;
            var target = context.Configuration.GetOrAddOpenClConfiguration().UseEmulation ? "hw_emu" : "hw";

            var hardwareFrameworkPath = Path.GetFullPath(context.Configuration.HardwareFrameworkPath);
            var tmpDirectoryPath = EnsureDirectoryExists(GetTmpDirectoryPath(hashId));
            var xclbinFilePath = Path.Combine(tmpDirectoryPath, $"hastip.{target}.xclbin");
            var xclbinDirectoryPath = EnsureDirectoryExists(
                GetRtlDirectoryPath(hardwareFrameworkPath, hashId),
                "xclbin");

            return (hardwareFrameworkPath, tmpDirectoryPath, xclbinFilePath, xclbinDirectoryPath);
        }
    }
}
