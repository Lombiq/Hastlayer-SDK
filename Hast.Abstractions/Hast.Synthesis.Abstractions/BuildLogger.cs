using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Hast.Common.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hast.Common.Helpers.FileSystemHelper;

namespace Hast.Synthesis.Abstractions
{
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

        public Task ExecuteWithLogging(string executable, IList<string> arguments, string workingDirectory = null)
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
                _progressInvoker.InvokeProgress(new BuildProgressEventArgs(text));
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
    }

    public static class BuildLogger
    {
        public static (BuildLogger<T> BuildLogger, TextWriter BuildOutput) Create<T>(
            ILogger<T> logger,
            T progressInvoker,
            string outFileName = "build")
            where T: IProgressInvoker
        {
            string buildOutputPath = null;
            TextWriter buildOutput = null;

            var buildOutputDirectoryPath = EnsureDirectoryExists("App_Data", "logs");
            for (var i = 0; i < 100 && buildOutput == null; i++)
            {
                var fileName = i == 0 ? $"{outFileName}.out" : $"{outFileName}~{i}.out";
                buildOutputPath = Path.Combine(buildOutputDirectoryPath, fileName);

                try
                {
                    buildOutput = new StreamWriter(buildOutputPath, append: false, Encoding.UTF8);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Failed to open {0} for writing.", fileName);
                }
            }

            if (string.IsNullOrEmpty(buildOutputPath))
            {
                throw new InvalidOperationException("Failed to initialize the build output path.");
            }

            var buildLogger = new BuildLogger<T>(logger, buildOutputPath, progressInvoker, buildOutput);

            return (buildLogger, buildOutput);
        }

        public static void OnProgress<T>(ILogger<T> logger, int majorStepCount, int total, BuildProgressEventArgs e) =>
            logger.LogInformation(
                total == 0 ? "Message on build step {0}{3}: {2}" : "Message on build step {0}/{1}{3}: {2}",
                majorStepCount,
                total,
                e.Message,
                e.IsMajorStep ? " (new)" : string.Empty);
    }
}
