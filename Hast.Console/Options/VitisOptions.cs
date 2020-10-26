using CommandLine;

namespace Hast.Console.Options
{
    public class VitisOptions
    {
        [Option("hash", HelpText = "The hash, also known as transformation ID.")]
        public string Hash { get; set; }

        [Value(0, HelpText = "Sub-command name, here vitis.")]
        public string Subcommand { get; set; }

        [Value(1, HelpText = "Instruction name. (available: help, build)")]
        public string Instruction { get; set; }
    }
}
