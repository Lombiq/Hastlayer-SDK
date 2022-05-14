using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Hast.Common.Helpers;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lombiq.HelpfulLibraries.Common.Utilities.FileSystemHelper;

namespace Hast.Synthesis.Abstractions;

public class BuildLogger<T>
{
    private const string Vpp = "v++";
    private readonly string[] _vppStatusLogs = { "] Starting ", "] Phase ", "] Finished " };

    private readonly ILogger<T> _logger;
    private readonly string _buildOutputPath;
    private readonly IProgressInvoker _progressInvoker;
    private readonly TextWriter _buildOutput;

    public BuildLogger(
        ILogger<T> logger,
        string buildOutputPath,
        IProgressInvoker progressInvoker,
        TextWriter buildOutput)
    {
        _logger = logger;
        _buildOutputPath = buildOutputPath;
        _progressInvoker = progressInvoker;
        _buildOutput = buildOutput;
    }

    public Task ExecuteWithLoggingAsync(
        string executable,
        IList<string> arguments,
        string workingDirectory = null,
        TextWriter outputWriter = null)
    {
        var name = Path.GetFileName(executable);

        var hasWorkingDirectory = Directory.Exists(workingDirectory);
        Command Configure(Command command)
        {
            if (hasWorkingDirectory) command = command.WithWorkingDirectory(workingDirectory!);
            return command.WithValidation(CommandResultValidation.None);
        }

        _logger.LogInformation(
            "Starting program: {Executable} {Arguments} (working directory: {WorkingDirectory})",
            executable,
            string.Join(" ", arguments),
            hasWorkingDirectory ? workingDirectory : ".");
        return CliHelper.StreamAsync(
            executable,
            arguments,
            commandEvent => OnCommandEvent(commandEvent, name, arguments, outputWriter),
            Configure);
    }

    private void OnCommandEvent(
        CommandEvent commandEvent,
        string name,
        IEnumerable<string> arguments,
        TextWriter outputWriter)
    {
        switch (commandEvent)
        {
            case StartedCommandEvent started:
                Log(
                    LogLevel.None,
                    name,
                    FormattableString.Invariant($"#{started.ProcessId} arguments:\n\t{string.Join("\n\t", arguments)}"),
                    "started");
                break;
            case StandardOutputCommandEvent output:
                outputWriter?.WriteLine(output.Text);
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
                        StringHelper.Join(
                            " ",
                            $"The command {name} exited with code {exited.ExitCode}.",
                            $"You can review the output at '{Path.GetFullPath(_buildOutputPath)}'."));
                }

                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown {nameof(CommandEvent)} type \"{commandEvent.GetType().Name}\".");
        }
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

        //// Raise the v++ status outputs like "[21:17:26] Phase 1 Build RT Design" trough the Progress event.
        if (name == Vpp && text?.StartsWithOrdinal("[") == true && _vppStatusLogs.Any(fragment => text.Contains(fragment)))
        {
            if (logLevel < LogLevel.Information) logLevel = LogLevel.Information;
            _progressInvoker.InvokeProgress(new BuildProgressEventArgs(text));
        }

        if (logLevel == LogLevel.Error && text?.Contains("Failed to finish platform linker") == true)
        {
            throw new InvalidOperationException(
                "The linker encountered an error. This is typically because the resulting hardware design won't " +
                "fit on the FPGA as it's too complex. Try to make your code simpler (make it shorter, use " +
                "smaller data types and a lower degree of parallelism) until this error goes away.");
        }

        _logger.Log(logLevel, "{Name}: {Message}", name, message);
        _buildOutput.WriteLine("{0} {1}: {2}", name, buildLogType, message);
    }
}

public static class BuildLogger
{
    public static (BuildLogger<T> BuildLogger, TextWriter BuildOutput) Create<T>(
        ILogger<T> logger,
        T progressInvoker,
        string outFileName = "build")
        where T : IProgressInvoker
    {
        string buildOutputPath = null;
        TextWriter buildOutput = null;

        var buildOutputDirectoryPath = EnsureDirectoryExists("App_Data", "logs");
        for (var i = 0; i < 100 && buildOutput == null; i++)
        {
            var fileName = i == 0
                ? FormattableString.Invariant($"{outFileName}.out")
                : FormattableString.Invariant($"{outFileName}~{i}.out");
            buildOutputPath = Path.Combine(buildOutputDirectoryPath, fileName);

            try
            {
                buildOutput = new StreamWriter(buildOutputPath, append: false, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Failed to open {FileName} for writing.", fileName);
            }
        }

        if (string.IsNullOrEmpty(buildOutputPath))
        {
            throw new InvalidOperationException("Failed to initialize the build output path.");
        }

        var buildLogger = new BuildLogger<T>(logger, buildOutputPath, progressInvoker, buildOutput);

        return (buildLogger, buildOutput);
    }

    public static void OnProgress<T>(ILogger<T> logger, int majorStepCount, int total, BuildProgressEventArgs e)
    {
        if (total == 0)
        {
            logger.LogInformation(
                "Message on build step {MajorStepCount}{IsMajorStep}: {Message}",
                majorStepCount,
                e.IsMajorStep ? " (new)" : string.Empty,
                e.Message);
        }
        else
        {
            logger.LogInformation(
                "Message on build step {MajorStepCount}/{Total}{IsMajorStep}: {Message}",
                majorStepCount,
                total,
                e.IsMajorStep ? " (new)" : string.Empty,
                e.Message);
        }
    }
}
