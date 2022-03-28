using CommandLine;
using Hast.Console.Attributes;
using Hast.Console.Extensions;
using Hast.Console.Options;
using Hast.Console.Subcommands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static System.Console;

namespace Hast.Console;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This application is not localized.")]
internal class Program
{
    private static Dictionary<string, SubcommandInfo> _subcommands;

    private static void RunOptions(MainOptions mainOptions)
    {
        if (mainOptions.ListCommands)
        {
            var allSubcommands = string.Join("\n* ", _subcommands.Keys);
            WriteLine("Subcommands:\n* {0}", allSubcommands);
        }
        else if (mainOptions.Subcommand?.ToUpperInvariant() is { } name &&
                 _subcommands.SingleOrDefault(sub => sub.Key.ToUpperInvariant() == name) is { })
        {
            WriteLine("Please put the subcommand name as the first argument!");
        }
        else
        {
            WriteLine("Nothing to do.");
        }
    }

    private static void HandleParseError(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();

        // This is from an interactive Console, we want to exit.
#pragma warning disable S1147 // Exit methods should not be called
        if (errorList.Any(error => error.Tag == ErrorType.HelpRequestedError)) Environment.Exit(0);
#pragma warning restore S1147 // Exit methods should not be called

        if (errorList.Any())
        {
            WriteLine("Bad arguments.");
            ReadKey();
        }
    }

    private static void Main(string[] args)
    {
        _subcommands = typeof(SubcommandAttribute)
            .GetTypesWithAttribute()
            .Select(result => new SubcommandInfo
            {
                CommandName = ((SubcommandAttribute)result.Attribute)!.Name,
                Instance = (ISubcommand)result.Type!
                        .GetConstructor(new[] { typeof(string[]) })!
                    .Invoke(new object[] { args }),
            })
            .ToDictionary(info => info.CommandName);

        if (_subcommands.TryGetValue(args[0], out var subcommand))
        {
            subcommand.Instance.Run();
            return;
        }

        Parser.Default.ParseArguments<MainOptions>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }

    private sealed class SubcommandInfo
    {
        public string CommandName { get; set; }
        public ISubcommand Instance { get; set; }
    }
}
