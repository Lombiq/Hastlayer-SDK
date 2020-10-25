﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Hast.Vitis.Abstractions.Models.XilinxReport;

namespace Hast.Vitis.Abstractions.Models
{
    public class XilinxReportSection
    {
        private const string TableBorderLine = "+---";

        private readonly string[][] _data;

        public int Rows { get; }
        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<string> Comments { get; }


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


        public static XilinxReportSection Parse(TextReader reader)
        {
            // Scroll to the table start. Each section always seems to have a table even if empty.
            ReadUntil(reader, TableBorderLine);
            var columnNames = reader.ReadLine()!.Trim('|').Split('|').Select(columnName => columnName.Trim())
                .ToList();
            ReadUntil(reader, TableBorderLine);

            // Read each line into a table and then drop the bottom table border line.
            var data = ReadWhile(reader, line => line.StartsWith("|") && !line.StartsWith(TableBorderLine))
                .Select(line => line.Trim('|')
                    .Split('|')
                    .Select(cell => cell.Trim())
                    .ToArray())
                .ToArray();

            // Post-table comments start with an asterisk.
            var comments = new List<string>();
            while (reader.ReadLine() is { } line && line.StartsWith("*")) comments.Add(line);

            return new XilinxReportSection(data, columnNames, comments);
        }
    }
}
