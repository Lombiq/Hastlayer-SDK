using CommandLine;
using Hast.Common.Models;
using Hast.Console.Attributes;
using Hast.Console.Options;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Services;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static System.Console;

namespace Hast.Console.Subcommands
{
    [Subcommand("vitis")]
    public class VitisSubcommand : ISubcommand
    {
        private readonly MainOptions _mainOptions;
        private readonly string[] _rawArguments;

        public ILogger<VitisHardwareImplementationComposerBuildProvider> BuildLogger { get; set; } =
            new NullLogger<VitisHardwareImplementationComposerBuildProvider>();

        public VitisSubcommand(MainOptions mainOptions, string[] rawArguments)
        {
            _mainOptions = mainOptions;
            _rawArguments = rawArguments;
        }

        private void HandleParseError(IEnumerable<Error> obj)
        {
        }

        private async Task RunOptionsAsync(VitisOptions options)
        {
            switch (Enum.Parse<Instruction>(options.Instruction, ignoreCase: true))
            {
                case Instruction.Build:
                    var vhdlPath = _mainOptions.InputFilePath;
                    if (vhdlPath?.EndsWith(".vhdl") != true || !File.Exists(vhdlPath))
                    {
                        throw new ArgumentException("Please set the -i option to point to the main vhdl file! " +
                                                    "(eg. ./HardwareFramework/rtl/src/IP/Hast_IP.vhd)");
                    }

                    var outputSet = !string.IsNullOrWhiteSpace(_mainOptions.OutputFilePath);
                    if (outputSet && !_mainOptions.OutputFilePath.EndsWith(".xclbin"))
                    {
                        throw new ArgumentException("Please set the -o option to point to the desired location of the " +
                                                    "xclbin file (eg. ./HardwareFramework/bin/Hastlayer.) or omit it!");
                    }

                    var manifest = new XilinxDeviceManifest();
                    var transformationId = "Hastlayer";
                    var context = new HardwareImplementationCompositionContext
                    {
                        DeviceManifest = manifest,
                        Configuration = new HardwareGenerationConfiguration(
                            manifest.Name,
                            Path.GetFullPath("HardwareFramework")),
                        HardwareDescription = new VhdlHardwareDescription
                        {
                            HardwareEntryPointNamesToMemberIdMappings = new Dictionary<string, int>(),
                            TransformationId = transformationId,
                            Warnings = Array.Empty<ITransformationWarning>(),
                        },
                    };
                    var implementation = new HardwareImplementation
                    {
                        BinaryPath = outputSet
                            ? _mainOptions.OutputFilePath
                            : Path.Combine("VitisBuildOutput", transformationId + ".xclbin"),
                    };


                    await new VitisHardwareImplementationComposerBuildProvider(BuildLogger)
                        .BuildAsync(context, implementation);

                    WriteLine("Build Completed. Find files at: {0}", Path.GetFullPath(implementation.BinaryPath));

                    break;
                default:
                    System.Console.Error.WriteLine(
                        "The valid options are: {0}",
                        string.Join(", ", Enum.GetNames(typeof(Instruction))));
                    throw new ArgumentOutOfRangeException(options.Instruction);
            }
        }

        public void Run() =>
            Parser.Default.ParseArguments<VitisOptions>(_rawArguments)
                .WithParsed(options => RunOptionsAsync(options).Wait())
                .WithNotParsed(HandleParseError);

        private enum Instruction
        {
            Build
        }
    }
}
