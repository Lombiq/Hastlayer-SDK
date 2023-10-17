using CommandLine;
using Hast.Communication.Exceptions;
using Hast.Communication.Services;
using Hast.Communication.Tester.Helpers;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Synthesis.Services;
using Hast.Transformer;
using Hast.Transformer.SimpleMemory;
using Lombiq.HelpfulLibraries.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Communication.Tester;

[SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "This is a simple tester utility, there is no need for localization.")]
public static class Program
{
    public const string DefaultHexdumpFileName = "dump.txt";
    public const string DefaultBinaryFileName = "dump.bin";

    public const int HexDumpBlocksPerLine = 8;

    public static Options CommandLineOptions { get; set; }

    private static Hastlayer _hastlayer;

    private static void OnServiceGeneration(object sender, IServiceCollection services) =>
        services.RemoveImplementations<ITransformer>();

    private static async Task MainTaskAsync(IServiceProvider provider)
    {
        var hastlayer = provider.GetService<IHastlayer>();

        // Get devices and if asked exit with the device list.
        var devices = provider.GetService<IDeviceManifestSelector>().GetSupportedDevices()?.ToList();
        if (devices?.Any() != true) throw new InvalidOperationException("No devices are available!");

        if (CommandLineOptions.ListDevices)
        {
            foreach (var d in devices) Console.WriteLine(d.Name);
            return;
        }

        // If there is an output file name, then the file type can not be None.
        if (CommandLineOptions.OutputFileType == OutputFileType.None &&
            !string.IsNullOrEmpty(CommandLineOptions.OutputFileName))
        {
            CommandLineOptions.OutputFileType = OutputFileType.Hexdump;
        }

        // Try to load selected device or pick the first available if none were selected.
        if (string.IsNullOrEmpty(CommandLineOptions.DeviceName)) CommandLineOptions.DeviceName = devices[0].Name;
        var selectedDevice = devices.Find(device => device.Name == CommandLineOptions.DeviceName) ??
            throw new InvalidOperationException($"Target device '{CommandLineOptions.DeviceName}' not found!");
        var channelName = selectedDevice.DefaultCommunicationChannelName;

        var prepend = Array.Empty<int>();
        if (!string.IsNullOrEmpty(CommandLineOptions.Prepend))
        {
            prepend = CommandLineOptions.Prepend.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToArray();
        }

        var hardwareGenerationConfiguration = new HardwareGenerationConfiguration(selectedDevice.Name);
        var (memory, accessor) = await GenerateMemoryAsync(
            hastlayer,
            hardwareGenerationConfiguration,
            CommandLineOptions.PayloadType,
            CommandLineOptions.PayloadLengthCells,
            prepend,
            CommandLineOptions.InputFileName);

        // Save input to file using the format of the output file type.
        await SaveFileAsync(
            CommandLineOptions.OutputFileType,
            CommandLineOptions.PayloadType,
            CommandLineOptions.InputFileName,
            isInput: true,
            memory);

        // Create reference copy of input to compare against output.
        SimpleMemory referenceMemory = null;
        if (!CommandLineOptions.NoCheck)
        {
            Memory<byte> newMemory = new byte[memory.ByteCount];
            accessor.Get().CopyTo(newMemory);
            referenceMemory = SimpleMemory.CreateSoftwareMemory(memory.CellCount);
            if (!string.IsNullOrEmpty(CommandLineOptions.ReferenceAction))
            {
                string name = CommandLineOptions.ReferenceAction.ToUpperInvariant();
                var type = typeof(MemoryTest)
                    .Assembly
                    .GetTypes()
                    .Single(currentType =>
                        currentType.Name.ToUpperInvariant() == name &&
                        currentType.GetConstructor(Array.Empty<Type>()) != null &&
                        GetReferenceAction(currentType) != null);
                var sample = type.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
                GetReferenceAction(type)?.Invoke(sample, new object[] { referenceMemory });
            }
        }

        Console.WriteLine("Starting hardware execution.");
        var communicationService = provider.GetService<ICommunicationServiceSelector>().GetCommunicationService(channelName);
        communicationService.TesterOutput = Console.Out;
        var executionContext = new BasicExecutionContext(
            hastlayer,
            selectedDevice.Name,
            selectedDevice.DefaultCommunicationChannelName);
        var info = await communicationService.ExecuteAsync(memory, CommandLineOptions.MemberId, executionContext);

        Console.WriteLine(
            "Executing test on hardware took {0:0.##} ms (net) {1:0.##} ms (all together)",
            info.HardwareExecutionTimeMilliseconds,
            info.FullExecutionTimeMilliseconds);

        // Save output to file.
        await SaveFileAsync(
            CommandLineOptions.OutputFileType,
            CommandLineOptions.PayloadType,
            CommandLineOptions.OutputFileName,
            isInput: false,
            memory);

        if (!string.IsNullOrWhiteSpace(CommandLineOptions.JsonOutputFileName))
        {
            var json = JsonConvert.SerializeObject(new { Success = true, Result = info });
            await File.WriteAllTextAsync(CommandLineOptions.JsonOutputFileName, json);
        }

        // Verify results if wanted.
        if (!CommandLineOptions.NoCheck) Verify(memory, referenceMemory);
    }

    // In at least one case (ImageContrastModifier) Run can't be public because it would cause issues with the
    // transformation.
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
    private static MethodInfo GetReferenceAction(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SingleOrDefault(
                methodInfo => methodInfo.Name == nameof(MemoryTest.Run) &&
                methodInfo.GetParameters().Length == 1 &&
                methodInfo.GetParameters()[0].ParameterType == typeof(SimpleMemory));
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

    private static async Task<(SimpleMemory Memory, SimpleMemoryAccessor Accessor)> GenerateMemoryAsync(
        IHastlayer hastlayer,
        IHardwareGenerationConfiguration configuration,
        PayloadType type,
        int cellCount,
        int[] prependCells,
        string inputFileName)
    {
        Console.WriteLine("Generating memory.");
        var memory = hastlayer.CreateMemory(configuration, prependCells.Length + cellCount);
        var accessor = new SimpleMemoryAccessor(memory);

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
                var random = new NonSecurityRandomizer();
                for (int i = prependCells.Length; i < memory.CellCount; i++) memory.WriteInt32(i, random.Get());

                break;
            case PayloadType.BinaryFile:
                accessor.Load(inputFileName, memory.PrefixCellCount);
                break;
            case PayloadType.Bitmap:
                await using (var stream = File.OpenRead(inputFileName))
                {
                    using var bitmap = await Image.LoadAsync<Rgba32>(stream);

                    memory = BitmapHelper.ToSimpleMemory(configuration, hastlayer, bitmap, prependCells);
                    accessor = new SimpleMemoryAccessor(memory);
                }

                break;
            default:
                throw new ArgumentException($"Unknown payload type: {type}.");
        }

        return (memory, accessor);
    }

    private static async Task SaveFileAsync(
        OutputFileType fileType,
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
                    await using var streamWriter = new StreamWriter(fileName, append: false, Encoding.UTF8);
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
            case OutputFileType.BitmapJpeg:
                await using (var stream = File.OpenRead(CommandLineOptions.InputFileName))
                {
                    using var input = await Image.LoadAsync<Rgba32>(stream, new BmpDecoder());
                    using var output = BitmapHelper.FromSimpleMemory(memory, input, CommandLineOptions.Prepend?.Length ?? 0);

                    await output.SaveAsync(fileName, new JpegEncoder());
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
            Console.WriteLine(new HardwareExecutionResultMismatchException(mismatches, memory.CellCount));
        }

        if (memory.CellCount != referenceMemory.CellCount)
        {
            Console.WriteLine(
                "MISMATCH IN LENGTH:{0}Hardware: {1}{0}Software: {2}",
                Environment.NewLine,
                memory.CellCount,
                referenceMemory.CellCount);
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
                writer.Write("{0}{1:X8}", j == 0 ? string.Empty : " ", memory.ReadUInt32(i + j));
            }

            writer.WriteLine();
        }
    }

    private static void Main(string[] args)
    {
        try
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => CommandLineOptions = options);
            if (CommandLineOptions == null) return;

            var hastlayerConfiguration = new HastlayerConfiguration();
            hastlayerConfiguration.OnServiceRegistration += OnServiceGeneration;

            // It's not in the interface because the feature is for internal use only.
            _hastlayer = Hastlayer.Create(hastlayerConfiguration);

            _hastlayer.RunAsync<IServiceProvider>(MainTaskAsync).Wait();
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
            else
            {
                Console.WriteLine(ex.InnerExceptions[0]);
            }
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
