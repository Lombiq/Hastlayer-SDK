using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Models
{
    public class XilinxReport
    {
        private const string HorizontalLine = "----";

        public IDictionary<string, string> MetaData { get; } =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public IDictionary<string, XilinxReportSection> Sections { get; } =
            new Dictionary<string, XilinxReportSection>(StringComparer.InvariantCultureIgnoreCase);


        public static async Task<XilinxReport> ParseAsync(TextReader reader)
        {
            var report = new XilinxReport();

            // Read the meta data (block starts with a horizontal line).
            await ReadUntilAsync(reader);
            foreach (var metaLine in await ReadWhileAsync(reader, line => !line.StartsWith(HorizontalLine, StringComparison.Ordinal)))
            {
                if (!metaLine.Contains(':')) continue;
                var parts = metaLine.TrimStart('|').Split(new[] { ':' }, 2);
                report.MetaData[parts[0].Trim()] = parts[1].Trim();
            }

            // Get all section headers from the Table of Contents.
            await ReadUntilAsync(reader, "Table of Contents");
            await ReadUntilAsync(reader);
            var sectionHeaders = await ReadWhileAsync(reader, line => !string.IsNullOrWhiteSpace(line));

            foreach (var title in sectionHeaders)
            {
                // Scroll to section start with underlined header that starts with the chapter number.
                var header = await ReadUntilAsync(reader, title);
                if (header == null) break;
                await ReadUntilAsync(reader);

                report.Sections[header] = await XilinxReportSection.ParseAsync(reader, header);
            }

            return report;
        }


        public static async Task<string> ReadUntilAsync(TextReader reader, string start = HorizontalLine)
        {
            do
            {
                var line = await reader.ReadLineAsync();
                if (line?.StartsWith(start, StringComparison.InvariantCulture) != false) return line;
            } while (true);
        }

        public static async Task<List<string>> ReadWhileAsync(
            TextReader reader,
            Func<string, bool> condition,
            bool includeLast = false)
        {
            var results = new List<string>();
            do
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (!condition(line))
                {
                    if (includeLast) results.Add(line);
                    break;
                }

                results.Add(line);
            } while (true);

            return results;
        }
    }
}
