using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;
using static Hast.Vitis.Abstractions.Constants.Extensions;
using static Hast.Vitis.Abstractions.Services.VitisHardwareImplementationComposerBuildProvider;

namespace Hast.Vitis.Abstractions.Services
{
    public sealed class ZynqHardwareImplementationComposerBuildProvider
        : IHardwareImplementationComposerBuildProvider, IDisposable
    {
        private readonly ILogger<ZynqHardwareImplementationComposerBuildProvider> _logger;
        private readonly BuildLogger<ZynqHardwareImplementationComposerBuildProvider> _buildLogger;
        private readonly TextWriter _buildOutput;

        private int MajorStep { get; set; }

        public IDictionary<string, BuildProviderShortcut> Shortcuts { get; } =
            new Dictionary<string, BuildProviderShortcut>();

        public ISet<string> Requirements { get; } = new HashSet<string>
        {
            nameof(VitisHardwareImplementationComposerBuildProvider),
        };

        public ZynqHardwareImplementationComposerBuildProvider(
            ILogger<ZynqHardwareImplementationComposerBuildProvider> logger)
        {
            _logger = logger;
            (_buildLogger, _buildOutput) = BuildLogger.Create(logger, this, "build-zynq");
        }

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.DeviceManifest is ZynqDeviceManifest;

        public async Task BuildAsync(
            IHardwareImplementationCompositionContext context,
            IHardwareImplementation implementation)
        {
            var (hardwareFrameworkPath, tmpDirectoryPath, xclbinFilePath, _) = GetBuildPaths(context);

            var xclbinutilExecutable = await GetExecutablePathAsync("xclbinutil");
            Task ExecuteXclbinutil(TextWriter output, params string[] arguments) =>
                _buildLogger.ExecuteWithLoggingAsync(xclbinutilExecutable, arguments, tmpDirectoryPath, output);

            var python3Executable = await GetExecutablePathAsync("python3");
            Task ExecutePython3(params string[] arguments) =>
                _buildLogger.ExecuteWithLoggingAsync(python3Executable, arguments, tmpDirectoryPath);

            var binaryFilePath = GetBinaryPath(context.Configuration, context.HardwareDescription);
            var bitFilePath = xclbinFilePath.Replace(".xclbin", ".bit");

            // mv $(XCLBIN)/hastip.$(TARGET).xclbin $(XCLBIN)/hastip.$(TARGET).xclbin.org
            // cp ./src/IP/Hast_IP.vhd.name $(XCLBIN)/
            // cp ./src/IP/Hast_IP.vhd.hash $(XCLBIN)/
            // @$(VIVADO) -mode batch -source ./src/scripts/scale_frequency.tcl ./_x/link/vivado/vpl/prj/prj.xpr -tclargs ./xclbin myxpr
            // @xclbinutil --input ./xclbin/hastip.hw.xclbin.org --replace-section CLOCK_FREQ_TOPOLOGY:json:./xclbin/clock_freq_topology.json \
            //             --output ./xclbin/hastip.hw.xclbin --force
            // @xclbinutil --input ./xclbin/hastip.hw.xclbin --info ./xclbin/hastip.hw.xclbin.info --force
            // @xclbinutil --input ./xclbin/hastip.hw.xclbin --dump-section BITSTREAM:RAW:./xclbin/hastip.hw.bit --force
            // @python3 ./src/scripts/FPGA-BIT-TO-BIN.PY -f ./xclbin/hastip.hw.bit ./xclbin/hastip.hw.bit.bin

            if (File.Exists(xclbinFilePath)) File.Move(xclbinFilePath, xclbinFilePath + ".org");

            await File.WriteAllTextAsync(Path.Combine(tmpDirectoryPath, "Hast_IP.vhd.name"), context.Configuration.Label);
            await File.WriteAllTextAsync(Path.Combine(tmpDirectoryPath, "Hast_IP.vhd.hash"), context.HardwareDescription.TransformationId);
            var tmpXclbinDirectoryPath = EnsureDirectoryExists(tmpDirectoryPath, "xclbin");
            MajorProgress($"The xclbin file was moved to {xclbinFilePath}.org.");

            var vivadoExecutable = await GetExecutablePathAsync("vivado");
            var vivadoArguments = new[]
            {
                "-mode",
                "batch",
                "-source",
                GetScriptFile(hardwareFrameworkPath, "scale_frequency.tcl"),
                Path.Combine(tmpDirectoryPath, "_x/link/vivado/vpl/prj/prj.xpr"),
                "-tclargs",
                tmpXclbinDirectoryPath,
                "myxpr",
            };
            await _buildLogger.ExecuteWithLoggingAsync(vivadoExecutable, vivadoArguments, tmpDirectoryPath);
            MajorProgress("Frequency scaling profile created.");

            await ExecuteXclbinutil(
                output: null,
                "--input",
                xclbinFilePath + ".org",
                "--replace-section",
                "CLOCK_FREQ_TOPOLOGY:json:" + Path.Join(tmpXclbinDirectoryPath, "clock_freq_topology.json"),
                "--output",
                xclbinFilePath,
                "--force");
            MajorProgress("Xclbinutil update completed. (1/3)");
            await ExecuteXclbinutil(
                output: null,
                "--input",
                xclbinFilePath,
                "--info",
                xclbinFilePath + ".info",
                "--force");
            MajorProgress("Xclbinutil update completed. (2/3)");
            await ExecuteXclbinutil(
                output: null,
                "--input",
                xclbinFilePath,
                "--dump-section",
                "BITSTREAM:RAW:" + bitFilePath,
                "--force");
            MajorProgress("Xclbinutil update completed. (3/3)");

            await using (var infoFileWriter =
                new StreamWriter(binaryFilePath + InfoFileExtension, append: false, Encoding.UTF8))
            {
                await ExecuteXclbinutil(
                    output: infoFileWriter,
                    "--input",
                    xclbinFilePath,
                    "--info");
                MajorProgress("Xclbinutil info file recreated.");
            }

            await ExecutePython3(
                GetScriptFile(hardwareFrameworkPath, "fpga-bit-to-bin.py"),
                "-f",
                bitFilePath,
                bitFilePath + ".bin");
            MajorProgress("Frequency scaling in bin file completed.");

            var binFilePath = GetBitBinPath(context);

            File.Copy(bitFilePath + ".bin", binFilePath);

            await File.WriteAllTextAsync(binaryFilePath + SetScaleExtension, "/sys/devices/soc0/fclk0/set_rate");
        }

        public void AddShortcuts(IEnumerable<IHardwareImplementationComposerBuildProvider> providers)
        {
            foreach (var provider in providers)
            {
                if (provider.Name is
                    nameof(VitisHardwareImplementationComposerBuildProvider) or
                    nameof(ZynqHardwareImplementationComposerBuildProvider))
                {
                    provider
                        .Shortcuts
                        .Add(
                        nameof(ZynqHardwareImplementationComposerBuildProvider),
                        context => File.Exists(GetBitBinPath(context)));
                }
            }
        }

        public void InvokeProgress(BuildProgressEventArgs eventArgs)
        {
            if (eventArgs.IsMajorStep) MajorStep++;
            BuildLogger.OnProgress(_logger, MajorStep, total: 0, eventArgs);
        }

        private void MajorProgress(string message) =>
            InvokeProgress(new BuildProgressEventArgs(message, isMajorStep: true));

        private static string GetBitBinPath(IHardwareImplementationCompositionContext context) =>
            GetBinaryPath(context.Configuration, context.HardwareDescription).Replace(".xclbin", ".bit.bin");

        public void Dispose() => _buildOutput?.Dispose();
    }
}
