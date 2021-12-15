using CommandLine;
using Hast.Console.Attributes;
using Hast.Console.Extensions;
using Hast.Console.Options;
using Hast.Console.Subcommands;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace Hast.Console
{
    internal class Program
    {
        private static Dictionary<string, SubcommandInfo> _subcommands;

        private static void RunOptions(MainOptions mainOptions, string[] arguments)
        {
            if (mainOptions.ListCommands)
            {
                var allSubcommands = string.Join("\n* ", _subcommands.Keys);
                WriteLine("Subcommands:\n* {0}", allSubcommands);
            }
            else if (mainOptions.Subcommand?.ToUpperInvariant() is { } name &&
                     _subcommands.SingleOrDefault(sub => sub.Key.ToUpperInvariant() == name) is { } subcommand)
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

            if (errorList.Any(x => x.Tag == ErrorType.HelpRequestedError)) Environment.Exit(0);

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
                        .Invoke(new object[] { args })
                })
                .ToDictionary(info => info.CommandName);

            if (_subcommands.TryGetValue(args[0], out var subcommand))
            {
                subcommand.Instance.Run();
                return;
            }

            Parser.Default.ParseArguments<MainOptions>(args)
                .WithParsed(options => RunOptions(options, args))
                .WithNotParsed(HandleParseError);
        }

        private class SubcommandInfo
        {
            public string CommandName { get; set; }
            public ISubcommand Instance { get; set; }
        }
    }
}
