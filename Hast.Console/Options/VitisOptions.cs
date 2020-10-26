using CommandLine;

namespace Hast.Console.Options
{
    public class VitisOptions : MainOptions
    {
        [Option("platform", HelpText = "The plaform directory name.", Required = true)]
        public string Platform { get; set; }

        [Option("hash", HelpText = "The hash, also known as transformation ID.")]
        public string Hash { get; set; }

        [Value(1, HelpText = "Instruction name. (available: help, build)")]
        public string Instruction { get; set; }
    }
}
