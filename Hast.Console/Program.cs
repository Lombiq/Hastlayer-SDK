using CommandLine;
using Hast.Console.Attributes;
using Hast.Console.Extensions;
using Hast.Console.Options;
using Hast.Console.Services;
using Hast.Console.Subcommands;
using Hast.Layer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace Hast.Console
{
    internal class Program
    {
        private readonly HashSet<string> _subcommands;

        private readonly IStringLocalizer T;

        private Program(IServiceProvider provider, IEnumerable<string> subcommands)
        {
            _subcommands = subcommands.ToHashSet();

            T = provider.GetRequiredService<IStringLocalizer<Program>>();
        }

        private void RunOptions(MainOptions mainOptions)
        {
            if (mainOptions.ListCommands)
            {
                var allSubcommands = string.Join("\n* ", _subcommands);
                WriteLine(T["Subcommands:\n* {0}", allSubcommands]);
            }
            else if (mainOptions.Subcommand?.ToUpperInvariant() is { } name &&
                     _subcommands.SingleOrDefault(sub => sub.ToUpperInvariant() == name) is { })
            {
                WriteLine(T["Please put the subcommand name as the first argument!"]);
            }
            else
            {
                WriteLine(T["Nothing to do."]);
            }
        }

        private void HandleParseError(IEnumerable<Error> errors)
        {
            var errorList = errors.ToList();

            if (errorList.Any(x => x.Tag == ErrorType.HelpRequestedError)) return;

            if (errorList.Any())
            {
                WriteLine(T["Bad arguments."]);
                ReadKey();
            }
        }

        private static void Main(string[] args)
        {
            var subcommands = typeof(SubcommandAttribute)
                .GetTypesWithAttribute()
                .ToDictionary(
                    result => ((SubcommandAttribute)result.Attribute)!.Name,
                    result => result.Type,
                    StringComparer.InvariantCultureIgnoreCase);

            using var hastlayer = Hastlayer.Create(new HastlayerConfiguration
            {
                OnServiceRegistration = (_, services) =>
                {
                    services.AddLocalization();
                    services.AddSingleton<IArgumentsAccessor>(new ArgumentsAccessor(args));

                    foreach (var type in subcommands.Values)
                    {
                        services.AddSingleton(typeof(ISubcommand), type);
                        services.AddSingleton(type);
                    }
                },
            });

            // It's not in the interface because the feature is for internal use only.
#pragma warning disable S3215 // "interface" instances should not be cast to concrete types
            ((Hastlayer)hastlayer).RunGet(provider =>
            {
                if (args.Length > 0 && subcommands.TryGetValue(args[0], out var subcommandType))
                {
                    ((ISubcommand)provider.GetRequiredService(subcommandType)).Run();
                    return true;
                }

                var program = new Program(provider, subcommands.Keys);
                Parser.Default.ParseArguments<MainOptions>(args)
                    .WithParsed(options => program.RunOptions(options))
                    .WithNotParsed(program.HandleParseError);
                return true;
            });
#pragma warning restore S3215 // "interface" instances should not be cast to concrete types
        }
    }
}
