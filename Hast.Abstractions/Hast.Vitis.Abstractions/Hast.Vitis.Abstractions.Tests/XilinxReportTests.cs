using Hast.Vitis.Abstractions.Models;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Hast.Vitis.Abstractions.Tests
{
    public class XilinxReportTests
    {
        private static readonly IDictionary<string, string> _expectedMetaData = new Dictionary<string, string>
        {
            ["Tool Version"] = "Vivado v.2019.2 (lin64) Build 2708876 Wed Nov  6 21:39:14 MST 2019",
            ["Date"] = "Sun Oct 25 03:39:12 2020",
            ["Host"] = "cluster1 running 64-bit Ubuntu 18.04.5 LTS",
            ["Command"] = "report_utilization -file /tmp/david.el-saig/u250/HardwareFramework/reports/36a0b0eaefd20bf74638ef99659296a7a9f6c354e4dec750d7ae19275f7ea087/Hast_IP_synth_util.rpt",
            ["Design"] = "Hast_IP",
            ["Device"] = "xcu250figd2104-2L",
            ["Design State"] = "Synthesized",
        };

        private static readonly string[] _expectedSections = {
            "1. CLB Logic",
            "1.1 Summary of Registers by Type",
            "2. BLOCKRAM",
            "3. ARITHMETIC",
            "4. I/O",
            "5. CLOCK",
            "6. ADVANCED",
            "7. CONFIGURATION",
            "8. Primitives",
            "9. Black Boxes",
            "10. Instantiated Netlists",
            "11. SLR Connectivity",
            "12. SLR Connectivity Matrix",
            "13. SLR CLB Logic and Dedicated Block Utilization",
            "14. SLR IO Utilization",
        };

        private static readonly string[] _expectedColumns = {
            "Site Type",
            "Used",
            "Fixed",
            "Available",
            "Util%",
        };

        private static readonly Dictionary<string, string>[] _expectedTableData =
        {
            new Dictionary<string, string>
            {
                ["Site Type"] = "CLB LUTs*",
                ["Used"] = "25034",
                ["Fixed"] = "0",
                ["Available"] = "1728000",
                ["Util%"] = "1.45",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "LUT as Logic",
                ["Used"] = "25034",
                ["Fixed"] = "0",
                ["Available"] = "1728000",
                ["Util%"] = "1.45",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "LUT as Memory",
                ["Used"] = "0",
                ["Fixed"] = "0",
                ["Available"] = "791040",
                ["Util%"] = "0.00",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "CLB Registers",
                ["Used"] = "10098",
                ["Fixed"] = "0",
                ["Available"] = "3456000",
                ["Util%"] = "0.29",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "Register as Flip Flop",
                ["Used"] = "10098",
                ["Fixed"] = "0",
                ["Available"] = "3456000",
                ["Util%"] = "0.29",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "Register as Latch",
                ["Used"] = "0",
                ["Fixed"] = "0",
                ["Available"] = "3456000",
                ["Util%"] = "0.0",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "CARRY8",
                ["Used"] = "2258",
                ["Fixed"] = "0",
                ["Available"] = "216000",
                ["Util%"] = "1.05",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "F7 Muxes",
                ["Used"] = "48",
                ["Fixed"] = "0",
                ["Available"] = "864000",
                ["Util%"] = "<0.01",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "F8 Muxes",
                ["Used"] = "0",
                ["Fixed"] = "0",
                ["Available"] = "432000",
                ["Util%"] = "0.00",
            },
            new Dictionary<string, string>
            {
                ["Site Type"] = "F9 Muxes",
                ["Used"] = "0",
                ["Fixed"] = "0",
                ["Available"] = "216000",
                ["Util%"] = "0.00",
            },
        };

        [Fact]
        public void SampleShouldParseAsExpected()
        {
            using var reader = File.OpenText("sample.rpt");
            var report = XilinxReport.Parse(reader);

            report.MetaData.ShouldNotBeEmpty();
            report.Sections.ShouldNotBeEmpty();

            foreach (var (key, value) in _expectedMetaData) report.MetaData[key].ShouldBe(value);

            report.Sections.Keys.OrderBy(x => x).ToArray().ShouldBe(_expectedSections.OrderBy(x => x).ToArray());

            var section = report.Sections["1. CLB Logic"];
            section.Columns.ToArray().ShouldBe(_expectedColumns);
            section.Rows.ShouldBe(_expectedTableData.Length);
            for (var i = 0; i < section.Rows; i++)
            {
                _expectedColumns
                    .Select(column => section[i][column])
                    .ToArray()
                    .ShouldBe(_expectedColumns.Select(column => section[i][column]).ToArray());
            }
        }

        [Fact]
        public void SummaryShouldWork()
        {
            using var reader = File.OpenText("sample2.rpt");
            var report = XilinxReport.Parse(reader);

            report
                .Sections["1. Summary"]
                .ToDictionaryByFirstColumn()["Max Ambient (C)"][XilinxReportSection.Value]
                .ShouldBe("87.1");
        }
    }
}
