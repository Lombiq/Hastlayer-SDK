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
    class Program
    {
        private static void RunOptions(MainOptions mainOptions, string[] arguments)
        {
            var subcommands = typeof(SubcommandAttribute)
                .GetTypesWithAttribute()
                .Select(result => new
                {
                    CommandName = ((SubcommandAttribute)result.Attribute)!.Name,
                    Instance = (ISubcommand)result.Type!
                            .GetConstructor(new[] { typeof(MainOptions), typeof(string[]) })!
                        .Invoke(new object[] { mainOptions, arguments }),
                });


            if (mainOptions.ListCommands)
            {
                WriteLine("Subcommands:\n* {0}", string.Join("\n* ", subcommands.Select(c => c.CommandName)));
            }
            else if (mainOptions.Subcommand?.ToUpperInvariant() is { } name &&
                     subcommands.SingleOrDefault(c => c.CommandName.ToUpperInvariant() == name) is { } subcommand)
            {
                subcommand.Instance.Run();
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

        private static void Main(string[] args) =>
            Parser.Default.ParseArguments<MainOptions>(args)
                .WithParsed(options => RunOptions(options, args))
                .WithNotParsed(HandleParseError);
    }
}
