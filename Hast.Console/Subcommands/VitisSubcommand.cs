using CommandLine;
using CommandLine.Text;
using Hast.Common.Models;
using Hast.Console.Attributes;
using Hast.Console.Options;
using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Models;
using Hast.Vitis.Abstractions.Models;
using Hast.Vitis.Abstractions.Services;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Console;

namespace Hast.Console.Subcommands;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This application is not localized.")]
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
            .WithParsed(options => RunOptionsAsync(options, result, BuildLogger).Wait())
            .WithNotParsed(errors => WriteLine(string.Join("\n", errors)));
    }

    private static async Task RunOptionsAsync(
        VitisOptions options,
        ParserResult<VitisOptions> parserResult,
        ILogger<VitisHardwareImplementationComposerBuildProvider> logger)
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
                return;
            case Instruction.Build:
                await BuildAsync(options, logger);
                return;
            case Instruction.Json:
                await JsonAsync(options);
                return;
            default:
                // CLI arguments can be lower case.
#pragma warning disable CA1308
                var validOptions = string.Join(
                    ", ",
                    Enum.GetNames(typeof(Instruction)).Select(value => value.ToLowerInvariant()));
#pragma warning restore CA1308
                await System.Console.Error.WriteLineAsync($"The valid options are: {validOptions}");
                throw new ArgumentOutOfRangeException(options.Instruction);
        }
    }

    private static Task BuildAsync(VitisOptions options, ILogger<VitisHardwareImplementationComposerBuildProvider> logger)
    {
        var hardwareFrameworkPath = options.InputFilePath ?? "HardwareFramework";
        if (!Directory.Exists(hardwareFrameworkPath))
        {
            throw new ArgumentException("Please set the -i option to point to the directory that contains the " +
                                        "rtl directory! (eg. ./HardwareFramework)");
        }

        var outputSet = !string.IsNullOrWhiteSpace(options.OutputFilePath);
        if (outputSet && !options.OutputFilePath.EndsWithOrdinalIgnoreCase(".xclbin"))
        {
            throw new ArgumentException("Please set the -o option to point to the location of the xclbin file " +
                                        "(eg. ./VitisOutput/Hastlayer.xclbin) or omit it!");
        }

        var manifest = new VitisDeviceManifest { SupportedPlatforms = new[] { options.Platform ?? string.Empty } };
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

        return BuildInnerAsync(context, implementation, logger);
    }

    private static async Task BuildInnerAsync(
        HardwareImplementationCompositionContext context,
        HardwareImplementation implementation,
        ILogger<VitisHardwareImplementationComposerBuildProvider> logger)
    {
        using var buildProvider = new VitisHardwareImplementationComposerBuildProvider(logger);
        await buildProvider.BuildAsync(context, implementation);

        WriteLine("Build Completed. Find files under: {0}", Path.GetFullPath(implementation.BinaryPath));
    }

    private static Task JsonAsync(VitisOptions options)
    {
        var input = new FileInfo(options.InputFilePath);
        if (!input.Exists)
        {
            throw new ArgumentException("Please set the -i option to file you wish to convert.");
        }

        if (input.Extension == ".rpt") return RptToJsonAsync(options, input);

        return Task.CompletedTask;
    }

    private static async Task RptToJsonAsync(VitisOptions options, FileInfo input)
    {
        using var reader = File.OpenText(input.FullName);
        var report = await XilinxReport.ParseAsync(reader);
        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        await File.WriteAllTextAsync(options.OutputFilePath ?? "report.json", json);
    }

    private enum Instruction
    {
        Help,
        Build,
        Json,
    }
}
