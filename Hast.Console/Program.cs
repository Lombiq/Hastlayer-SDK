using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Diagnostics;
using System.IO;
using CommandLine.Text;

namespace Hast.Console
{
    class Program
    {
        private static void RunOptions(Options options)
        {
            if (options.Help) Environment.Exit(0);
        }
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            var errorList = errors.ToList();

            if (errorList.Any(x => x.Tag == ErrorType.HelpRequestedError)) Environment.Exit(0);

            if (errorList.Any())
            {
                System.Console.WriteLine("Bad arguments.");
                System.Console.ReadKey();
            }
        }

        private static void Main(string[] args)
        {
            var options = new Options();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);

            //Console.ReadKey();
        }
    }
}
