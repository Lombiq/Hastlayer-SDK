using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace Hast.Console
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Path to the input file to transform.")]
        public string InputFilePath { get; set; }

        // For future use
        [Option('f', "full", HelpText = "Full transforming.")]
        public bool Full { get; set; }

        [Option('h', "help", HelpText = "Displays help.")]
        public bool Help { get; set; }
    }
}
