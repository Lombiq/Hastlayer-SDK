using CommandLine;
using Hast.Communication.Exceptions;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Communication.Tester
{
    partial class Program
    {
        public const string DefaultHexdumpFileName = "dump.txt";
        public const string DefaultBinaryFileName = "dump.bin";

        public const int HexDumpBlocksPerLine = 8;


        private static async Task MainTask(Options configuration)
        {
            using var hastlayer = Hastlayer.Create(new HastlayerConfiguration { Flavor = HastlayerFlavor.Developer });

            // Get devices and if asked exit with the device list.
            var devices = hastlayer.GetSupportedDevices();
            if (devices == null || !devices.Any()) throw new Exception("No devices are available!");

            if (configuration.ListDevices)
            {
                foreach (var d in devices) Console.WriteLine(d.Name);
                return;
            }


            // If there is an output file name, then the file type can not be None.
            if (configuration.OutputFileType == OutputFileType.None && !string.IsNullOrEmpty(configuration.OutputFileName))
                configuration.OutputFileType = OutputFileType.Hexdump;


            // Try to load selected device or pick the first available if none were selected.
            if (string.IsNullOrEmpty(configuration.DeviceName)) configuration.DeviceName = devices.First().Name;
            var selectedDevice = devices.FirstOrDefault(device => device.Name == configuration.DeviceName);
            if (selectedDevice == null) throw new Exception($"Target device '{configuration.DeviceName}' not found!");
            var channelName = selectedDevice.DefaultCommunicationChannelName;


                var (memory, accessor) = GenerateMemory(
                    configuration.PayloadType, configuration.PayloadLengthCells, configuration.InputFileName);


            // Save input to file using the format of the output file type.
            SaveFile(configuration.OutputFileType, configuration.PayloadType, configuration.InputFileName, true, memory);

            // Create reference copy of input to compare against output.
            var referenceMemory = configuration.NoCheck ? null : SimpleMemoryAccessor.Create(accessor.Get());

            Console.WriteLine("Starting hardware execution.");
            using var communicationServiceContainer = hastlayer.GetCommunicationService(channelName);
            var communicationService = communicationServiceContainer.Value;
            communicationService.TesterOutput = Console.Out;
            var executionContext = new BasicExecutionContext(hastlayer, selectedDevice.Name,
                selectedDevice.DefaultCommunicationChannelName);
            var info = await communicationService.Execute(memory, configuration.MemberId, executionContext);

            Console.WriteLine("Executing test on hardware took {0:0.##}ms (net) {1:0.##}ms (all together)",
                info.HardwareExecutionTimeMilliseconds, info.FullExecutionTimeMilliseconds);

            // Save output to file.
            SaveFile(configuration.OutputFileType, configuration.PayloadType, configuration.OutputFileName, false, memory);

            if (!string.IsNullOrWhiteSpace(configuration?.JsonOutputFileName))
            {
                var json = JsonConvert.SerializeObject(new { Success = true, Result = info });
                File.WriteAllText(configuration.JsonOutputFileName, json);
            }


            // Verify results if wanted.
            if (!configuration.NoCheck) Verify(memory, referenceMemory);
        }


        private static (SimpleMemory, SimpleMemoryAccessor) GenerateMemory(PayloadType type, int cellCount, string inputFileName)
        {
            Console.WriteLine("Generating memory.");
            var memory = new SimpleMemory(cellCount);
            var accessor = new SimpleMemoryAccessor(memory);
            switch (type)
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
                case PayloadType.BinaryFile:
                    accessor.Load(inputFileName, memory.PrefixCellCount);
                    break;
                default:
                    throw new ArgumentException($"Unknown payload type: {type}.");
            }

            return (memory, accessor);
        }

        private static void SaveFile(OutputFileType fileType,
            PayloadType payloadType,
            string fileName,
            bool isInput,
            SimpleMemory memory)
        {
            var fileNamePrefix = isInput ? "in-" : "out-";
            var direction = isInput ? "input" : "output";

            switch (fileType)
            {
                case OutputFileType.None: return;
                case OutputFileType.Hexdump:
                    if (string.IsNullOrEmpty(fileName)) fileName = fileNamePrefix + DefaultHexdumpFileName;
                    Console.WriteLine("Saving {0} hexdump to '{1}'...", direction, fileName);
                    if (fileName == Options.OutputFileNameConsole)
                    {
                        WriteHexdump(Console.Out, memory);
                    }
                    else
                    {
                        using var streamWriter = new StreamWriter(fileName, false, Encoding.UTF8);
                        WriteHexdump(streamWriter, memory);
                    }
                    break;
                case OutputFileType.Binary:
                    if (payloadType != PayloadType.BinaryFile)
                    {
                        if (string.IsNullOrEmpty(fileName)) fileName = fileNamePrefix + DefaultBinaryFileName;
                        Console.WriteLine("Saving {0} binary file to '{1}'...", direction, fileName);
                        new SimpleMemoryAccessor(memory).Store(fileName);
                    }
                    break;
                default:
                    throw new ArgumentException(string.Format("Unknown {0} file type: {1}", direction, fileType));
            }

            Console.WriteLine("File saved.");
        }

        private static void Verify(SimpleMemory memory, SimpleMemory referenceMemory)
        {
            var mismatches = new List<HardwareExecutionResultMismatchException.Mismatch>();
            for (int i = 0; i < memory.CellCount && i < referenceMemory.CellCount; i++)
            {
                if (!memory.Read4Bytes(i).SequenceEqual(referenceMemory.Read4Bytes(i)))
                {
                    mismatches.Add(new HardwareExecutionResultMismatchException.Mismatch(
                        i, memory.Read4Bytes(i), referenceMemory.Read4Bytes(i)));
                }
            }

            if (mismatches.Count > 0)
            {
                Console.WriteLine("MISMATCH:");
                Console.WriteLine(new HardwareExecutionResultMismatchException(mismatches));
            }
            if (memory.CellCount != referenceMemory.CellCount)
            {
                Console.WriteLine("MISMATCH IN LENGTH:{0}Hardware: {1}{0}Software: {2}",
                    Environment.NewLine, memory.CellCount, referenceMemory.CellCount);
            }
            else if (mismatches.Count == 0)
            {
                Console.WriteLine("Verification passed!");
            }
        }

        public static void WriteHexdump(TextWriter writer, SimpleMemory memory)
        {
            for (int i = 0; i < memory.CellCount; i += HexDumpBlocksPerLine)
            {
                for (int j = 0; j < HexDumpBlocksPerLine && i + j < memory.CellCount; j++)
                {
                    writer.Write("{0}{1:X8}", j == 0 ? "" : " ", memory.ReadUInt32(i + j));
                }
                writer.WriteLine();
            }
        }


        private static void Main(string[] args)
        {
            Options configuration = null;
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { configuration = o; });
                if (configuration == null) throw new ArgumentNullException(nameof(configuration));
                MainTask(configuration).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (!string.IsNullOrWhiteSpace(configuration?.JsonOutputFileName))
                {
                    var json = JsonConvert.SerializeObject(new { Success = false, Exception = ex });
                    File.WriteAllText(configuration.JsonOutputFileName, json);
                }
            }

            if (Debugger.IsAttached) Console.ReadKey();
        }
    }
}
