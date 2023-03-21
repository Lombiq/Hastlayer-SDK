using Hast.Layer;
using Hast.Synthesis;
using Hast.Synthesis.Models;
using Hast.Synthesis.Services;
using System;
using System.Collections.Generic;

namespace Hast.Xilinx.Drivers;

public abstract class DeviceDriverBase : IDeviceDriver
{
    private readonly ITimingReportParser _timingReportParser;
    private readonly object _timingReportParserLock = new();

    protected Lazy<IDeviceManifest> _deviceManifest;
    private ITimingReport _timingReport;

    protected virtual string TimingReportFileName => null;

    public abstract string DeviceName { get; }

    public ITimingReport TimingReport
    {
        get
        {
            if (_timingReportParser == null) return null;

            lock (_timingReportParserLock)
            {
                _timingReport ??= _timingReportParser.Parse(
                    ResourceHelper.GetTimingReport(
                        TimingReportFileName ?? GetType().Name));

                return _timingReport;
            }
        }
    }

    public IDeviceManifest DeviceManifest => _deviceManifest.Value;

    protected DeviceDriverBase(ITimingReportParser timingReportParser) =>
        _timingReportParser = timingReportParser;

    public abstract void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);

    public HardwareGenerationConfiguration ToHardwareGenerationConfiguration(
        string hardwareFrameworkPath = null,
        IDictionary<string, object> customConfiguration = null,
        IList<string> hardwareEntryPointMemberFullNames = null,
        IList<string> hardwareEntryPointMemberNamePrefixes = null) =>
        new(
            DeviceName,
            hardwareFrameworkPath,
            customConfiguration,
            hardwareEntryPointMemberFullNames,
            hardwareEntryPointMemberNamePrefixes);
}
