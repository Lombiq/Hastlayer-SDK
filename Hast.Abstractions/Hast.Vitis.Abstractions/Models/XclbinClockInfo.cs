using Hast.Vitis.Abstractions.Interop.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Hast.Vitis.Abstractions.Models
{
    public class XclbinClockInfo
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public XclbinClockInfoType Type { get; set; }
        public uint Frequency { get; set; }

        public static IList<XclbinClockInfo> FromStream(Stream stream, Encoding encoding)
        {
            var results = new List<XclbinClockInfo>();
            using var reader = new StreamReader(stream, encoding);

            ReadUntilTitle(reader, "Clocks");

            var set = false;
            var item = new XclbinClockInfo();
            while (true)
            {
                var line = reader.ReadLine();

                // Stop at document end or at next header.
                var sectionFinished = line?.StartsWithOrdinal("---") != false;
                if (sectionFinished || string.IsNullOrWhiteSpace(line))
                {
                    if (set)
                    {
                        results.Add(item);
                        item = new XclbinClockInfo();
                        set = false;
                    }

                    if (sectionFinished) return results;
                    continue;
                }

                if (!line.Contains(":")) continue;

                ParseLine(line, item);

                set = true;
            }
        }

        private static void ParseLine(string line, XclbinClockInfo item)
        {
            var parts = line.Split(new[] { ':' }, 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case nameof(Name):
                    item.Name = value;
                    break;
                case nameof(Index):
                    item.Index = value.ToTechnicalInt();
                    break;
                case nameof(Type):
                    item.Type = (XclbinClockInfoType)Enum.Parse(typeof(XclbinClockInfoType), value, ignoreCase: true);
                    break;
                case nameof(Frequency):
                    parts = value.Split();
                    value = parts[0];

                    var number = uint.Parse(value, CultureInfo.InvariantCulture);
                    item.Frequency = parts[1].Trim().ToUpperInvariant() switch
                    {
                        "MHZ" => number * 1_000_000,
                        "KHZ" => number * 1_000,
                        "HZ" => number,
                        "" => number,
                        _ => throw new InvalidCastException($"Unknown frequency unit '{parts[1]}'."),
                    };

                    break;
                default: throw new InvalidDataException($"Unknown entry type: '{key}'.");
            }
        }

        private static void ReadUntilTitle(TextReader reader, string lineText)
        {
            while (reader.ReadLine() is { } line)
            {
                if (line == lineText)
                {
                    // Skip the "------" line.
                    reader.ReadLine();
                    return;
                }
            }
        }
    }
}
