using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hast.Vitis.Abstractions.Models
{
    public class XilinxReport
    {
        private const string HorizontalLine = "----";

        public IDictionary<string, string> MetaData { get; } =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public IDictionary<string, XilinxReportSection> Sections { get; } =
            new Dictionary<string, XilinxReportSection>(StringComparer.InvariantCultureIgnoreCase);


        public static XilinxReport Parse(TextReader reader)
        {
            var report = new XilinxReport();

            // Read the meta data (block starts with a horizontal line).
            ReadUntil(reader);
            foreach (var metaLine in ReadWhile(reader, line => !line.StartsWith(HorizontalLine, StringComparison.Ordinal)))
            {
                if (!metaLine.Contains(':')) continue;
                var parts = metaLine.TrimStart('|').Split(new[] { ':' }, 2);
                report.MetaData[parts[0].Trim()] = parts[1].Trim();
            }

            // Get all section headers from the Table of Contents.
            ReadUntil(reader, "Table of Contents");
            ReadUntil(reader);
            var sectionHeaders = ReadWhile(reader, line => !string.IsNullOrWhiteSpace(line)).ToList();

            foreach (var title in sectionHeaders)
            {
                // Scroll to section start with underlined header that starts with the chapter number.
                var header = ReadUntil(reader, title);
                if (header == null) break;
                ReadUntil(reader);

                report.Sections[header] = XilinxReportSection.Parse(reader, header);
            }

            return report;
        }


        public static string ReadUntil(TextReader reader, string start = HorizontalLine)
        {
            do
            {
                var line = reader.ReadLine();
                if (line?.StartsWith(start, StringComparison.InvariantCulture) != false) return line;
            } while (true);
        }

        public static IEnumerable<string> ReadWhile(
            TextReader reader,
            Func<string, bool> condition,
            bool includeLast = false)
        {
            do
            {
                var line = reader.ReadLine();
                if (line == null) yield break;

                if (!condition(line))
                {
                    if (includeLast) yield return line;
                    yield break;
                }

                yield return line;
            } while (true);
        }
    }
}
