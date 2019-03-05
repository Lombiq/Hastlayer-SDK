using CommandLine;
using Hast.Communication.Exceptions;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            //[Option('v', "verbose", HelpText = "Set output to verbose messages.")]
            //public bool Verbose { get; set; }

            [Option('l', "list", HelpText = "List available devices and exit.")]
            public bool ListDevices { get; set; }

            [Option('d', "device", HelpText = "Name of the selected device.")]
            public string DeviceName { get; set; }

            [Option('b', "bytes", HelpText = "The total size of the payload in bytes.")]
            public long PayloadBytes { get; set; } = 10;

            [Option('k', "kilo-bytes", HelpText = "The total size of the payload in kilobytes.")]
            public int PayloadKiloBytes { get => (int)(PayloadBytes / 1024); set => PayloadBytes = (long)value * 1024; }

            [Option('m', "mega-bytes", HelpText = "The total size of the payload in megabytes.")]
            public int PayloadLengthMegaBytes { get => (int)(PayloadBytes / 1024 / 1024); set => PayloadBytes = (long)value * 1024 * 1024; }

            [Option('c', "cells", HelpText = "The total size of the payload in number of cells.")]
            public int PayloadLengthCells
            {
                get => (int)(PayloadBytes / SimpleMemory.MemoryCellSizeBytes);
                set => PayloadBytes = (long)value * SimpleMemory.MemoryCellSizeBytes;
            }

            [Option('i', "member-id", HelpText = "The simlated MemberId.")]
            public int MemberId { get; set; } = 1;

            [Option('t', "payload-type", HelpText = "What kind of data to send (ConstantIntOne, Counter, Random)")]
            public PayloadType PayloadType { get; set; } = PayloadType.ConstantIntOne;

            [Option('f', "file-type", HelpText = "Output file type (None, Hexdump, Binary)")]
            public OutputFileType OutputFileType { get; set; } = OutputFileType.None;

            [Option('o', "output", HelpText = "Output file name. (overrides -f to Hexdump if it's None; use value '-' to write Hexdump to the console)")]
            public string OutputFileName { get; set; }

            [Option('j', "json", HelpText = "Create a summary as JSON file.")]
            public string JsonOutputFileName { get; set; }
        }

        public const string DefaultHexdumpFileName = "dump.txt";
        public const string DefaultBinaryFileName = "dump.bin";

        public const int HexDumpDigits = SimpleMemory.MemoryCellSizeBytes * 2;

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

                if (string.IsNullOrEmpty(configuration.DeviceName)) configuration.DeviceName = devices.First().Name;
                var selectedDevice = devices.FirstOrDefault(device => device.Name == configuration.DeviceName);
                if (selectedDevice == null) throw new Exception($"Target device '{configuration.DeviceName}' not found!");
                var channelName = selectedDevice.DefaultCommunicationChannelName;



                Console.WriteLine("Generating memory.");

                var memory = new SimpleMemory(configuration.PayloadLengthCells);
                var accessor = new SimpleMemoryAccessor(memory);
                switch (configuration.PayloadType)
                {
                    case PayloadType.ConstantIntOne:
                        for (int i = 0; i < memory.CellCount; i++) memory.WriteInt32(i, 1);
                        break;
                    case PayloadType.Counter:
                        for (int i = 0; i < memory.CellCount; i++) memory.WriteInt32(i, i);
                        break;
                    case PayloadType.Random:
                        var random = new Random();
                        for (int i = 0; i < memory.CellCount; i++)
                            memory.WriteInt32(i, random.Next(int.MinValue, int.MaxValue));
                        break;
                }
                var input = accessor.Get();

                // Create reference copy of input to compare against output.
                var referenceMemory = new SimpleMemory(memory.CellCount);
                var memoryBytes = new SimpleMemoryAccessor(memory).Get();
                memoryBytes.CopyTo(new SimpleMemoryAccessor(referenceMemory).Get());

                Console.WriteLine("Starting hardware execution.");
                var communicationService = await hastlayer.GetCommunicationService(channelName);
                var executionContext = new BasicExecutionContext(hastlayer, selectedDevice.Name,
                    selectedDevice.DefaultCommunicationChannelName);
                var info = await communicationService.Execute(memory, configuration.MemberId, executionContext);

                Console.WriteLine("Executing test on hardware took {0}ms (net) {1}ms (all together)",
                    info.HardwareExecutionTimeMilliseconds, info.FullExecutionTimeMilliseconds);

                // Verify results!
                var mismatches = new List<HardwareExecutionResultMismatchException.Mismatch>();
                for (int i = 0; i < memory.CellCount; i++)
                    if (!memory.Read4Bytes(i).SequenceEqual(referenceMemory.Read4Bytes(i)))
                        mismatches.Add(new HardwareExecutionResultMismatchException.Mismatch(
                            i, memory.Read4Bytes(i), referenceMemory.Read4Bytes(i)));
                if (mismatches.Any()) throw new HardwareExecutionResultMismatchException(mismatches);

                var output = accessor.Get();


                if (configuration.OutputFileType == OutputFileType.None && !string.IsNullOrEmpty(configuration.OutputFileName))
                    configuration.OutputFileType = OutputFileType.Hexdump;
                switch (configuration.OutputFileType)
                {
                    case OutputFileType.None: break;
                    case OutputFileType.Hexdump:
                        if (string.IsNullOrEmpty(configuration.OutputFileName))
                            configuration.OutputFileName = DefaultHexdumpFileName;
                        Console.WriteLine("Saving hexdump to '{0}'", configuration.OutputFileName);
                        if (configuration.OutputFileName == "-")
                            WriteHexdump(Console.Out, memory);
                        else
                            using (var streamWriter = new StreamWriter(configuration.OutputFileName, false, Encoding.UTF8))
                                WriteHexdump(streamWriter, memory);
                        Console.WriteLine("File saved.");
                        break;
                    case OutputFileType.Binary:
                        if (string.IsNullOrEmpty(configuration.OutputFileName))
                            configuration.OutputFileName = DefaultBinaryFileName;
                        Console.WriteLine("Saving binary file to '{0}'", configuration.OutputFileName);
                        using (var fileStream = File.OpenWrite(configuration.OutputFileName))
                            fileStream.Write(output.GetUnderlyingArray().Array, 0, memory.ByteCount);
                        Console.WriteLine("File saved.");
                        break;
                }
            }
        }

        public static void WriteHexdump(TextWriter writer, SimpleMemory memory)
        {
            for (int i = 0; i < memory.CellCount; i += HexDumpDigits)
                writer.WriteLine(string.Join(" ", memory.ReadUInt32(i, HexDumpDigits)
                    .Select(x => x.ToString("X").PadLeft(HexDumpDigits, '0'))));
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
