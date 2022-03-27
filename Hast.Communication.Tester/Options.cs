using CommandLine;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;

namespace Hast.Communication.Tester;

public class Options
{
    // Be sure to update the when changing these!

    [Option('l', "list", HelpText = "List available devices and exit.")]
    public bool ListDevices { get; set; }

    [Option('d', "device", HelpText = "Name of the selected device.")]
    public string DeviceName { get; set; }

    [Option('b', "bytes", HelpText = "The total size of the payload in bytes.")]
    public long PayloadLengthBytes { get; set; } = 10;

    [Option('k', "kilo-bytes", HelpText = "The total size of the payload in kilobytes.")]
    public int PayloadKiloBytes { get => (int)(PayloadLengthBytes / 1_024); set => PayloadLengthBytes = (long)value * 1_024; }

    [Option('c', "cells", HelpText = "The total size of the payload in number of cells.")]
    public int PayloadLengthCells
    {
        get => (int)Math.Ceiling((double)PayloadLengthBytes / SimpleMemory.MemoryCellSizeBytes);
        set => PayloadLengthBytes = (long)value * SimpleMemory.MemoryCellSizeBytes;
    }

    [Option('m', "member-id", HelpText = "The simulated MemberId.")]
    public int MemberId { get; set; }

    [Option('t', "payload-type", HelpText = "What kind of data to send (ConstantIntOne, Counter, Random, BinaryFile, Bitmap).")]
    public PayloadType PayloadType { get; set; } = PayloadType.ConstantIntOne;

    [Option('f', "file-type", HelpText = "Type of the files where input and output are dumped to (None, Hexdump, Binary, BitmapJpeg).")]
    public OutputFileType OutputFileType { get; set; } = OutputFileType.None;

    [Option('i', "input", HelpText = "Generated data is saved to or payload is read from this file when using BinaryFile as file-type.")]
    public string InputFileName { get; set; }

    public const string OutputFileNameConsole = "CONSOLE";

    [Option('o', "output", HelpText = "Output file name. (overrides -f to Hexdump if it's None; use value '" +
        OutputFileNameConsole + "' to write Hexdump to the console)")]
    public string OutputFileName { get; set; }

    [Option('j', "json", HelpText = "Create a summary as JSON file.")]
    public string JsonOutputFileName { get; set; }

    [Option('n', "no-check", HelpText = "Skips result check at the end.")]
    public bool NoCheck { get; set; }

    [Option(
        'L',
        "log-level",
        HelpText = "Sets the logging level for 'hastlayer', 0 for most verbose, 6 for nothing. (Trace, Debug, " +
        "Info, Warning, Error, Critical, None)")]
    public int LogLevelInt { get => (int)LogLevel; set => LogLevel = (LogLevel)value; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [Option(
        'a',
        "action",
        HelpText = "What sample to run on the reference memory (e.g. MemoryTest). Note that the given sample " +
        "should have a method with the exact signature \"Run(SimpleMemory memory)\".")]
    public string ReferenceAction { get; set; }

    [Option('p', "prepend", HelpText = "Prepend a list of integers to the SimpleMemory.")]
    public string Prepend { get; set; }
}
