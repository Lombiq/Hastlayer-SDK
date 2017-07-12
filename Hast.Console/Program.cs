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
        static void Main(string[] args)
        {
            var options = new Options();

            var arguments = Parser.Default.ParseArguments<Options>(args);

            if (arguments.Errors.Any())
            {
                System.Console.WriteLine("Bad arguments.");
                System.Console.ReadKey();
                return;
            }

            if (arguments.Value.Help)
            {
                System.Console.Write(HelpText.AutoBuild(arguments));
            }



            //Console.ReadKey();
        }
    }
}
