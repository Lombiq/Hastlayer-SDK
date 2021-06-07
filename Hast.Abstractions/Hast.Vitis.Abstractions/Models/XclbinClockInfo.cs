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
            using var reader = PrepareReader(stream, encoding);

            var state = new State { Item = new XclbinClockInfo() };
            while (true)
            {
                var isFinished = ReadLine(state, results, reader.ReadLine());
                if (isFinished) return results;
            }
        }

        private static bool ReadLine(State state, List<XclbinClockInfo> results, string line)
        {
            // Stop at document end or at next header.
            var sectionFinished = line?.StartsWith("---", StringComparison.Ordinal) != false;
            if (sectionFinished || string.IsNullOrWhiteSpace(line))
            {
                if (state.Set)
                {
                    results.Add(state.Item);
                    state.Item = new XclbinClockInfo();
                    state.Set = false;
                }

                if (sectionFinished) return true;
                return false;
            }

            if (!line.Contains(":", StringComparison.Ordinal)) return false;

            var parts = line.Split(new[] { ':' }, 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case nameof(Name):
                    state.Item.Name = value;
                    break;
                case nameof(Index):
                    state.Item.Index = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case nameof(Type):
                    state.Item.Type = (XclbinClockInfoType)Enum.Parse(typeof(XclbinClockInfoType), value, true);
                    break;
                case nameof(Frequency):
                    parts = value.Split();
                    value = parts[0];
                    state.Item.Frequency = parts[1].Trim().ToUpperInvariant() switch
                    {
                        "MHZ" => uint.Parse(value, CultureInfo.InvariantCulture) * 1_000_000,
                        "KHZ" => uint.Parse(value, CultureInfo.InvariantCulture) * 1_000,
                        "HZ" => uint.Parse(value, CultureInfo.InvariantCulture),
                        "" => uint.Parse(value, CultureInfo.InvariantCulture),
                        _ => throw new InvalidCastException($"Unknown frequency unit '{parts[1]}'."),
                    };
                    break;
                default: throw new InvalidDataException($"Unknown entry type: '{key}'.");
            }

            state.Set = true;
            return false;
        }

        private static StreamReader PrepareReader(Stream stream, Encoding encoding)
        {
            var reader = new StreamReader(stream, encoding);

            while (reader.ReadLine() is { } line)
            {
                if (line != "Clocks") continue;

                // Skip the "------" line.
                reader.ReadLine();
                return reader;
            }

            return reader;
        }

        private class State
        {
            public bool Set { get; set; }
            public XclbinClockInfo Item { get; set; }
        }
    }
}
