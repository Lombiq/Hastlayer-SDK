using Hast.Catapult.Models;
using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using Hast.Xilinx;
using static Hast.Common.Constants.DataSize;
using static Hast.Common.Constants.Frequency;

namespace Hast.Catapult.Drivers;

public class CatapultDriver : IDeviceDriver
{
    public const string DeviceName = CatapultConstants.Catapult;

    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    private ITimingReport _timingReport;

    public IDeviceManifest DeviceManifest { get; } =
        new CatapultDeviceManifest
        {
            Name = DeviceName,
            ClockFrequencyHz = 150 * Mhz,
            // Since it's completely Catapult-specific, not using e.g. "PCIe" here.
            SupportedCommunicationChannelNames = new[] { DeviceName },
            // Right now the whole memory is not available due to one physical cell being equal to one logical one.
            AvailableMemoryBytes = 8 * GigaByte / 16,
        };

    public ITimingReport TimingReport
    {
        get
        {
            lock (_timingReportParserLock)
            {
                _timingReport ??= _timingReportParser.Parse(
                    ResourceHelper.GetTimingReport(
                        nameof(CatapultDriver),
                        typeof(CatapultDriver).Assembly));

                return _timingReport;
            }
        }
    }

    public CatapultDriver(ITimingReportParser timingReportParser) => _timingReportParser = timingReportParser;

    public void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration)
    {
        // Method intentionally left empty. There is nothing to do with this device.
    }
}
