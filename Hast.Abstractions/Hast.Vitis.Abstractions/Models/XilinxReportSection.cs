using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Hast.Vitis.Abstractions.Models.XilinxReport;

namespace Hast.Vitis.Abstractions.Models
{
    public class XilinxReportSection
    {
        public const string Key = nameof(Key);
        public const string Value = nameof(Value);
        private const string TableBorderLine = "+---";
        private static readonly List<string> SimpleColumns = new() { "Key", "Value" };

        private readonly string[][] _data;

        public int Rows { get; }
        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<string> Comments { get; }

        [JsonProperty]
        public IReadOnlyList<IReadOnlyList<string>> Data => _data;

        public IDictionary<string, string> this[int rowIndex] =>
            _data[rowIndex]
                .Select((item, columnIndex) => new { Cell = item, ColumnIndex = columnIndex })
                .ToDictionary(item => Columns[item.ColumnIndex], item => item.Cell);

        public XilinxReportSection(string[][] data, IReadOnlyList<string> columns, IReadOnlyList<string> comments)
        {
            _data = data;
            Rows = data.Length;
            Columns = columns;
            Comments = comments;
        }

        public IDictionary<string, IDictionary<string, string>> ToDictionaryByFirstColumn() =>
            Enumerable
                .Range(0, Rows)
                .Select(row => this[row])
                .ToDictionary(row => row[Columns[0]]);

        public static async Task<XilinxReportSection> ParseAsync(TextReader reader, string title)
        {
            var columnNames = SimpleColumns;

            // Scroll to the table start. Each section always seems to have a table even if empty.
            await ReadUntilAsync(reader, TableBorderLine);
            if (!title.EndsWith(". Summary", StringComparison.InvariantCulture))
            {
                columnNames = (await reader.ReadLineAsync())!.Trim('|').Split('|').Select(columnName => columnName.Trim())
                    .ToList();
                await ReadUntilAsync(reader, TableBorderLine);
            }

            // Read each line into a table and then drop the bottom table border line.
            var data = (await ReadWhileAsync(reader, line => line.StartsWith("|", StringComparison.Ordinal) && !line.StartsWith(TableBorderLine, StringComparison.Ordinal)))
                .Select(line => line.Trim('|')
                    .Split('|')
                    .Select(cell => cell.Trim())
                    .ToArray())
                .ToArray();

            // Post-table comments start with an asterisk.
            var comments = new List<string>();
            while (await reader.ReadLineAsync() is { } line && line.StartsWith("*", StringComparison.Ordinal)) comments.Add(line);

            return new XilinxReportSection(data, columnNames, comments);
        }
    }
}
