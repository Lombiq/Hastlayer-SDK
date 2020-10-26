using CommandLine;
using CommandLine.Text;
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
using System.Linq;
using System.Threading.Tasks;
using static System.Console;

namespace Hast.Console.Subcommands
{
    [Subcommand("vitis")]
    public class VitisSubcommand : ISubcommand
    {
        private readonly string[] _rawArguments;

        public ILogger<VitisHardwareImplementationComposerBuildProvider> BuildLogger { get; set; } =
            new NullLogger<VitisHardwareImplementationComposerBuildProvider>();

        public VitisSubcommand(string[] rawArguments) => _rawArguments = rawArguments;

        private void HandleParseError(IEnumerable<Error> obj)
        {
        }

        private async Task RunOptionsAsync(VitisOptions options, ParserResult<VitisOptions> parserResult)
        {
            if (!Enum.TryParse(options.Instruction, ignoreCase: true, out Instruction instruction))
            {
                instruction = (Instruction)(-1);
            }

            switch (instruction)
            {
                case Instruction.Help:
                    WriteLine("The vitis specific help:");
                    WriteLine(
                        HelpText.AutoBuild(
                            parserResult,
                            error => error,
                            example => example));
                    break;
                case Instruction.Build:
                    var hardwareFrameworkPath = options.InputFilePath ?? "HardwareFramework";
                    if (!Directory.Exists(hardwareFrameworkPath))
                    {
                        throw new ArgumentException("Please set the -i option to point to the directory that " +
                                                    "contains the rtl directory! (eg. ./HardwareFramework)");
                    }

                    var outputSet = !string.IsNullOrWhiteSpace(options.OutputFilePath);
                    if (outputSet && !options.OutputFilePath.EndsWith(".xclbin"))
                    {
                        throw new ArgumentException("Please set the -o option to point to the location of the xclbin " +
                                                    "file (eg. ./VitisOutput/Hastlayer.xclbin) or omit it!");
                    }

                    var manifest = new XilinxDeviceManifest { TechnicalName = options.Platform };
                    var context = new HardwareImplementationCompositionContext
                    {
                        DeviceManifest = manifest,
                        Configuration = new HardwareGenerationConfiguration(
                            manifest.Name,
                            Path.GetFullPath(hardwareFrameworkPath)),
                        HardwareDescription = new VhdlHardwareDescription
                        {
                            HardwareEntryPointNamesToMemberIdMappings = new Dictionary<string, int>(),
                            TransformationId = options.Hash ?? "Hastlayer",
                            Warnings = Array.Empty<ITransformationWarning>(),
                        },
                    };
                    var implementation = new HardwareImplementation
                    {
                        BinaryPath = outputSet
                            ? options.OutputFilePath
                            : Path.Combine("VitisOutput", context.HardwareDescription.TransformationId + ".xclbin"),
                    };


                    await new VitisHardwareImplementationComposerBuildProvider(BuildLogger)
                        .BuildAsync(context, implementation);

                    WriteLine("Build Completed. Find files at: {0}", Path.GetFullPath(implementation.BinaryPath));

                    break;
                default:
                    System.Console.Error.WriteLine(
                        "The valid options are: {0}",
                        string.Join(", ", Enum.GetNames(typeof(Instruction)).Select(value => value.ToLowerInvariant())));
                    throw new ArgumentOutOfRangeException(options.Instruction);
            }
        }

        public void Run()
        {
            var result = Parser.Default.ParseArguments<VitisOptions>(_rawArguments);
                result
                .WithParsed(options => RunOptionsAsync(options, result).Wait())
                .WithNotParsed(HandleParseError);
        }

        private enum Instruction
        {
            Help,
            Build,
        }
    }
}
