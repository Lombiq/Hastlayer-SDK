using CommandLine;
using CommandLine.Text;
using Hast.Common.Models;
using Hast.Console.Attributes;
using Hast.Console.Options;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Vitis.Abstractions.Models;
using Hast.Vitis.Abstractions.Services;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
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

        public void Run()
        {
            var result = Parser.Default.ParseArguments<VitisOptions>(_rawArguments);
            result
                .WithParsed(options => RunOptionsAsync(options, result).Wait())
                .WithNotParsed(errors => WriteLine(string.Join("\n", errors)));
        }

        private Task RunOptionsAsync(VitisOptions options, ParserResult<VitisOptions> parserResult)
        {
            if (!Enum.TryParse(options.Instruction, ignoreCase: true, out Instruction instruction))
            {
                instruction = (Instruction)(-1);
            }

            switch (instruction)
            {
                case Instruction.Help:
                    WriteLine("The Vitis-specific help:");
                    WriteLine(
                        HelpText.AutoBuild(
                            parserResult,
                            error => error,
                            example => example));
                    return Task.CompletedTask;
                case Instruction.Build: return BuildAsync(options);
                case Instruction.Json:
                    var input = new FileInfo(options.InputFilePath);
                    if (!input.Exists)
                    {
                        throw new ArgumentException("Please set the -i option to file you wish to convert.");
                    }

                    return JsonAsync(options, input);
                default: return ParseFailedAsync(options);
            }
        }

        private Task BuildAsync(VitisOptions options)
        {
            var hardwareFrameworkPath = options.InputFilePath ?? "HardwareFramework";
            if (!Directory.Exists(hardwareFrameworkPath))
            {
                throw new ArgumentException("Please set the -i option to point to the directory that contains the " +
                                            "rtl directory! (eg. ./HardwareFramework)");
            }

            var outputSet = !string.IsNullOrWhiteSpace(options.OutputFilePath);
            if (outputSet && !options.OutputFilePath.EndsWith(".xclbin", StringComparison.Ordinal))
            {
                throw new ArgumentException("Please set the -o option to point to the location of the xclbin file " +
                                            "(eg. ./VitisOutput/Hastlayer.xclbin) or omit it!");
            }

            var manifest = new XilinxDeviceManifest(true, options.Platform ?? string.Empty);
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

#pragma warning disable CA2000 // Dispose objects before losing scope
            var provider = new VitisHardwareImplementationComposerBuildProvider(BuildLogger);
#pragma warning restore CA2000 // Dispose objects before losing scope

            return provider
                .BuildAsync(context, implementation)
                .ThenAsync(() =>
                {
                    provider?.Dispose();
                    WriteLine(
                        "Build Completed. Find files under: {0}",
                        Path.GetFullPath(implementation.BinaryPath));
                });
        }

        private static async Task JsonAsync(VitisOptions options, FileInfo input)
        {
            if (input.Extension[1..] == "rpt")
            {
                using var reader = File.OpenText(input.FullName);
                var report = await XilinxReport.ParseAsync(reader);
                var json = JsonConvert.SerializeObject(report, Formatting.Indented);
                await File.WriteAllTextAsync(options.OutputFilePath ?? "report.json", json);
            }
            else
            {
                await System.Console.Error.WriteAsync($"Unknown extension type '{input.Extension}'. Supported: rpt");
            }
        }

        private static async Task ParseFailedAsync(VitisOptions options)
        {
            // That would be confusing.
#pragma warning disable CA1308 // Normalize strings to uppercase
            var validOptions = Enum.GetNames(typeof(Instruction)).Select(value => value.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
            await System.Console.Error.WriteLineAsync($"The valid options are: {string.Join(", ", validOptions)}");
            throw new ArgumentOutOfRangeException(options.Instruction);
        }

        private enum Instruction
        {
            Help,
            Build,
            Json,
        }
    }
}
