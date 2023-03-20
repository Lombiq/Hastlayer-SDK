using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx.Helpers;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Xilinx.Drivers;

public class AlveoU250Driver : IDeviceDriver
{
    public const string DeviceName = "Alveo U250";

    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    private ITimingReport _timingReport;

    public ITimingReport TimingReport
    {
        get
        {
            lock (_timingReportParserLock)
            {
                return _timingReport ??= _timingReportParser.Parse(ResourceHelper.GetTimingReport(nameof(AlveoU250Driver)));
            }
        }
    }

    public IDeviceManifest DeviceManifest { get; } =
        new VitisDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 300 * Mhz,
            SupportedCommunicationChannelNames = new[] { Constants.VitisCommunicationChannelName },
            // While there is 64GB DDR RAM the max object size in .NET is 2GB. So until we add paging to SimpleMemory
            // the limit is 2GB, see: https://github.com/Lombiq/Hastlayer-SDK/issues/27
            AvailableMemoryBytes = 2 * GigaByte,
            SupportsHbm = false,
            SupportedPlatforms = new[] { "xilinx_u250" },
        };

    public AlveoU250Driver(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration) =>
        MemoryConfigurationHelper.ConfigureMemoryForVitis(memory, hardwareGeneration);
}
