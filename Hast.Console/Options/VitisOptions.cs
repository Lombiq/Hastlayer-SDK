using CommandLine;

namespace Hast.Console.Options
{
    public class VitisOptions
    {
        [Value(1, HelpText = "Instruction name. (available: build)")]
        public string Instruction { get; set; }
    }
}
