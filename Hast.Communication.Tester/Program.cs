using CommandLine;
using Hast.Layer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Communication.Tester
{
    class Program
    {
        public enum PayloadType
        {
            /// <summary>
            /// Each cell contains an int value of 1 (00000001h)
            /// </summary>
            ConstantIntOne,

            /// <summary>
            /// Each cell had 1 larger value than the previous one, overflow is permitted.
            /// </summary>
            Counter,

            /// <summary>
            /// Each cell gets a random value.
            /// </summary>
            Random
        }

        public enum OutputFileType
        {
            /// <summary>
            /// No output file is to be generated.
            /// </summary>
            None,

            /// <summary>
            /// The output is saved as a text file containing a sequence of hexadecimal numbers in 8 digit groups.
            /// </summary>
            Hexdump,

            /// <summary>
            /// The output is saved as raw binary file.
            /// </summary>
            Binary
        }

        public class Options
        {
            [Option('v', "verbose", HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('l', "list", HelpText = "List available devices and exit.")]
            public bool ListDevices { get; set; }

            [Option('d', "device", HelpText = "Name of the selected device.")]
            public string DeviceName { get; set; }

            [Option('b', "bytes", HelpText = "The total size of the payload in bytes.")]
            public long PayloadBytes { get; set; } = 10;

            [Option('k', "kilo-bytes", HelpText = "The total size of the payload in kilobytes.")]
            public long PayloadKiloBytes { get => PayloadBytes / 1024; set => PayloadBytes = value * 1024; }

            [Option('m', "mega-bytes", HelpText = "The total size of the payload in megabytes.")]
            public long PayloadMegaBytes { get => PayloadBytes / 1024 / 1024; set => PayloadBytes = value * 1024 * 1024; }

            [Option('i', "member-id", HelpText = "The simlated MemberId.")]
            public int MemberId { get; set; } = 1;

            [Option('t', "payload-type", HelpText = "What kind of data to send (ConstantIntOne, Counter, Random)")]
            public PayloadType PayloadType { get; set; } = PayloadType.ConstantIntOne;

            [Option('f', "file-type", HelpText = "Output file type (None, Hexdump, Binary)")]
            public OutputFileType OutputFileType { get; set; } = OutputFileType.None;

            [Option('o', "output", HelpText = "Output file name. (overrides -f to Hexdump if it's None)")]
            public string OutputFileName { get; set; }
        }

        private static async Task MainTask(Options configuration)
        {
            using (var hastlayer = await Hastlayer.Create(new HastlayerConfiguration { Flavor = HastlayerFlavor.Developer }))
            {
                var devices = await hastlayer.GetSupportedDevices();
                if (devices == null || devices.Count() == 0) throw new Exception("No devices are available!");

                if (configuration.ListDevices)
                {
                    foreach (var d in devices) Console.WriteLine(d.Name);
                    return;
                }

                hastlayer.ExecutedOnHardware += (sender, e) =>
                {
                    Console.WriteLine(
                        "Executing " +
                        e.MemberFullName +
                        " on hardware took " +
                        e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                        " milliseconds (net) " +
                        e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                        " milliseconds (all together)");
                };


            }
        }

        private static void Main(string[] args)
        {
            try
            {
                Options configuration = null;
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { configuration = o; });
                if (configuration != null) MainTask(configuration).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadKey();
        }
    }
}
