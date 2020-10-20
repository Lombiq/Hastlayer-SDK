using CommandLine;

namespace Hast.Console.Options
{
    public class MainOptions
    {
        [Option('i', "input", HelpText = "Path to the input file to transform.")]
        public string InputFilePath { get; set; }

        [Option('o', "output", HelpText = "Path to the output file to transform.")]
        public string OutputFilePath { get; set; }

        [Option('c', "list-commands", HelpText = "List the available sub-commands.")]
        public bool ListCommands { get; set; }

        // For future use
        [Option('f', "full", HelpText = "Full transforming.")]
        public bool Full { get; set; }

        [Value(0, HelpText = "Sub-command name, if any.")]
        public string Subcommand { get; set; }

    }
}
