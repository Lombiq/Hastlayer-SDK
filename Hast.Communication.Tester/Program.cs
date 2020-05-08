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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Hast.Communication.Services;
using Hast.Samples.SampleAssembly;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hast.Communication.Tester
{
    public static class Program
    {
        public const string DefaultHexdumpFileName = "dump.txt";
        public const string DefaultBinaryFileName = "dump.bin";

        public const int HexDumpBlocksPerLine = 8;

        private static readonly Type[] SimpleMemoryArgument = new[] {typeof(SimpleMemory)};  

        public static Options CommandLineOptions { get; set; }

        private static IHastlayer _hastlayer { get; set; } = null;

        private static void OnServiceGeneration(object sender, IServiceCollection services)
        {
            services.RemoveImplementations<ITransformer>();
            services.AddScoped<ITransformer, NullTransformer>();

            services.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("hastlayer", CommandLineOptions.LogLevel)
                    .AddConsole();
            }));
            services.AddSingleton(provider => provider.GetService<ILoggerFactory>().CreateLogger("hastlayer"));
        }

        private static async Task MainTask(IServiceProvider provider)
        {
            
            // Get devices and if asked exit with the device list.
            var devices = provider.GetService<IDeviceManifestSelector>().GetSupportedDevices()?.ToList();
            if (devices?.Any() != true) throw new Exception("No devices are available!");

            if (CommandLineOptions.ListDevices)
            {
                foreach (var d in devices) Console.WriteLine(d.Name);
                return;
            }


            // If there is an output file name, then the file type can not be None.
            if (CommandLineOptions.OutputFileType == OutputFileType.None && !string.IsNullOrEmpty(CommandLineOptions.OutputFileName))
                CommandLineOptions.OutputFileType = OutputFileType.Hexdump;


            // Try to load selected device or pick the first available if none were selected.
            if (string.IsNullOrEmpty(CommandLineOptions.DeviceName)) CommandLineOptions.DeviceName = devices.First().Name;
            var selectedDevice = devices.FirstOrDefault(device => device.Name == CommandLineOptions.DeviceName);
            if (selectedDevice == null) throw new Exception($"Target device '{CommandLineOptions.DeviceName}' not found!");
            var channelName = selectedDevice.DefaultCommunicationChannelName;

            var prepend = Array.Empty<int>();
            if (!string.IsNullOrEmpty(CommandLineOptions.Prepend))
            {
                prepend = CommandLineOptions.Prepend.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray();
            }
            
            var (memory, accessor) = GenerateMemory(CommandLineOptions.PayloadType,
                CommandLineOptions.PayloadLengthCells, prepend, CommandLineOptions.InputFileName);


            // Save input to file using the format of the output file type.
            SaveFile(CommandLineOptions.OutputFileType, CommandLineOptions.PayloadType, CommandLineOptions.InputFileName, true, memory);

            // Create reference copy of input to compare against output.
            SimpleMemory referenceMemory = null;
            if (!CommandLineOptions.NoCheck)
            {
                    
                Memory<byte> newMemory = new byte[memory.ByteCount];
                accessor.Get().CopyTo(newMemory);
                referenceMemory = SimpleMemoryAccessor.Create(newMemory);
                if (!string.IsNullOrEmpty(CommandLineOptions.ReferenceAction))
                {
                    string name = CommandLineOptions.ReferenceAction.ToLower();
                    var type = typeof(MemoryTest)
                        .Assembly
                        .GetTypes()
                        .Single(x => x.Name.ToLower() == name &&
                                     x.GetMethod(nameof(MemoryTest.Run), SimpleMemoryArgument) != null &&
                                     x.GetConstructor(Array.Empty<Type>()) != null);
                    var sample = type.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
                    type.GetMethod(nameof(MemoryTest.Run), SimpleMemoryArgument)?.Invoke(sample, new object[] { referenceMemory });
                }
            }

            Console.WriteLine("Starting hardware execution.");
            var communicationService = provider.GetService<ICommunicationServiceSelector>().GetCommunicationService(channelName);
            communicationService.TesterOutput = Console.Out;
            var executionContext = new BasicExecutionContext(provider.GetService<IHastlayer>(), selectedDevice.Name,
                selectedDevice.DefaultCommunicationChannelName);
            var info = await communicationService.Execute(memory, CommandLineOptions.MemberId, executionContext);

            Console.WriteLine("Executing test on hardware took {0:0.##}ms (net) {1:0.##}ms (all together)",
                info.HardwareExecutionTimeMilliseconds, info.FullExecutionTimeMilliseconds);

            // Save output to file.
            SaveFile(CommandLineOptions.OutputFileType, CommandLineOptions.PayloadType, CommandLineOptions.OutputFileName, false, memory);

            if (!string.IsNullOrWhiteSpace(CommandLineOptions.JsonOutputFileName))
            {
                var json = JsonConvert.SerializeObject(new { Success = true, Result = info });
                File.WriteAllText(CommandLineOptions.JsonOutputFileName, json);
            }


            // Verify results if wanted.
            if (!CommandLineOptions.NoCheck) Verify(memory, referenceMemory);
        }


        private static (SimpleMemory, SimpleMemoryAccessor) GenerateMemory(
            PayloadType type,
            int cellCount,
            int[] prependCells,
            string inputFileName)
        {
            Console.WriteLine("Generating memory.");
            var prefixCellCount = 4;
            var (memory, accessor) = SimpleMemoryAccessor.Create(
                new byte[(prefixCellCount + prependCells.Length + cellCount) * SimpleMemory.MemoryCellSizeBytes], prefixCellCount);

            for (var i = 0; i < prependCells.Length; i++) memory.WriteInt32(i, prependCells[i]);
            
            switch (type)
            {
                case PayloadType.ConstantIntOne:
                    for (int i = prependCells.Length; i < memory.CellCount; i++) memory.WriteInt32(i, 1);
                    break;
                case PayloadType.Counter:
                    for (int i = prependCells.Length; i < memory.CellCount; i++) memory.WriteInt32(i, i);
                    break;
                case PayloadType.Random:
                    var random = new Random();
                    for (int i = prependCells.Length; i < memory.CellCount; i++)
                        memory.WriteInt32(i, random.Next(int.MinValue, int.MaxValue));
                    break;
                case PayloadType.BinaryFile:
                    using (var fileStream = File.OpenRead(inputFileName))
                    {
                        var extraBytes = (prependCells.Length + prefixCellCount) * SimpleMemory.MemoryCellSizeBytes;
                        var data = new byte[extraBytes + fileStream.Length];
                        fileStream.Read(data, extraBytes, (int)fileStream.Length);
                        Memory<byte> dataAsMemory = data;
                        MemoryMarshal.Cast<int, byte>(new Span<int>(prependCells))
                            .CopyTo(dataAsMemory.Span.Slice(prefixCellCount * SimpleMemory.MemoryCellSizeBytes));
                        
                        accessor.Set(dataAsMemory, prefixCellCount);
                    }
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
                        
                        using var fileStream = File.OpenWrite(fileName);
                        
                        var accessor = new SimpleMemoryAccessor(memory);
                        var segment = accessor.Get().GetUnderlyingArray();
                        fileStream.Write(segment.Array ?? Array.Empty<byte>(), segment.Offset, memory.ByteCount);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown {direction} file type: {fileType}");
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
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o => { CommandLineOptions = o; });
                if (CommandLineOptions == null) return;
                
                var hastlayerConfiguration = new HastlayerConfiguration {Flavor = HastlayerFlavor.Inert};
                hastlayerConfiguration.OnServiceRegistration += OnServiceGeneration; 
                _hastlayer = Hastlayer.Create(hastlayerConfiguration).Result;

                _hastlayer.RunAsync<IServiceProvider>(MainTask).Wait();
            }
            catch (AggregateException ex)
            {
                while (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is AggregateException inner) ex = inner;
                if (ex.InnerExceptions.Count > 1)
                {
                    Console.WriteLine("MULTIPLE EXCEPTIONS: {0}", ex.InnerExceptions.Count);
                    foreach (var exception in ex.InnerExceptions)
                    {
                        Console.WriteLine("\n===================================================================\n");
                        Console.WriteLine(exception);
                    }
                }
                else Console.WriteLine(ex.InnerExceptions[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var outputFileName = CommandLineOptions?.JsonOutputFileName;
                if (!string.IsNullOrWhiteSpace(outputFileName))
                {
                    var json = JsonConvert.SerializeObject(new { Success = false, Exception = ex });
                    File.WriteAllText(outputFileName, json);
                }
            }

            _hastlayer?.Dispose();
        }
    }
}
