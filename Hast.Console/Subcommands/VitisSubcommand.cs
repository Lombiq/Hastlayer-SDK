using CommandLine;
using Hast.Console.Attributes;
using Hast.Console.Options;
using System;
using System.Collections.Generic;

namespace Hast.Console.Subcommands
{
    [Subcommand("vitis")]
    public class VitisSubcommand : ISubcommand
    {
        private readonly MainOptions _mainOptions;
        private readonly string[] _rawArguments;

        public VitisSubcommand(MainOptions mainOptions, string[] rawArguments)
        {
            _mainOptions = mainOptions;
            _rawArguments = rawArguments;
        }

        private void HandleParseError(IEnumerable<Error> obj)
        {
        }

        private void RunOptions(VitisOptions options)
        {
            switch (Enum.Parse<Instruction>(options.Instruction, ignoreCase: true))
            {
                case Instruction.Build:

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
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);

        private enum Instruction
        {
            Build
        }
    }
}
