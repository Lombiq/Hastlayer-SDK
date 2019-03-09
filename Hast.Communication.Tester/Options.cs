using CommandLine;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;

namespace Hast.Communication.Tester
{
    public class Options
    {
        [Option('l', "list", HelpText = "List available devices and exit.")]
        public bool ListDevices { get; set; }

        [Option('d', "device", HelpText = "Name of the selected device.")]
        public string DeviceName { get; set; }

        [Option('b', "bytes", HelpText = "The total size of the payload in bytes.")]
        public long PayloadBytes { get; set; } = 10;

        [Option('k', "kilo-bytes", HelpText = "The total size of the payload in kilobytes.")]
        public int PayloadKiloBytes { get => (int)(PayloadBytes / 1024); set => PayloadBytes = (long)value * 1024; }

        [Option('c', "cells", HelpText = "The total size of the payload in number of cells.")]
        public int PayloadLengthCells
        {
            get => (int)Math.Ceiling((double)PayloadBytes / SimpleMemory.MemoryCellSizeBytes);
            set => PayloadBytes = (long)value * SimpleMemory.MemoryCellSizeBytes;
        }

        [Option('m', "member-id", HelpText = "The simulated MemberId.")]
        public int MemberId { get; set; } = 1;

        [Option('t', "payload-type", HelpText = "What kind of data to send (ConstantIntOne, Counter, Random, BinaryFile)")]
        public PayloadType PayloadType { get; set; } = PayloadType.ConstantIntOne;

        [Option('f', "file-type", HelpText = "Type of the files where input and output are dumped to(None, Hexdump, Binary)")]
        public OutputFileType OutputFileType { get; set; } = OutputFileType.None;

        [Option('i', "input", HelpText = "Generated data is saved to or payload is read from here when using BinaryFile as file-type.")]
        public string InputFileName { get; set; }

        [Option('o', "output", HelpText = "Output file name. (overrides -f to Hexdump if it's None; use value '-' to write Hexdump to the console)")]
        public string OutputFileName { get; set; }

        [Option('j', "json", HelpText = "Create a summary as JSON file.")]
        public string JsonOutputFileName { get; set; }
    }
}
