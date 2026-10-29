using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Synthesis.Helpers;
using Hast.Synthesis.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Synthesis;

/// <summary>
/// Provides FPGA-specific implementations.
/// </summary>
public interface IDeviceDriver : ISingletonDependency
{
    /// <summary>
    /// Gets the manifest with the device information.
    /// </summary>
    IDeviceManifest DeviceManifest { get; }

    /// <summary>
    /// Gets the report describing how long each operation takes.
    /// </summary>
    ITimingReport TimingReport { get; }

    /// <summary>
    /// Returns the amount of cycles required to perform the specified binary <paramref name="expression"/>.
    /// </summary>
    decimal GetClockCyclesNeededForBinaryOperation(BinaryOperatorExpression expression, int operandSizeBits, bool isSigned) =>
        DeviceDriverHelper.ComputeClockCyclesForBinaryOperation(DeviceManifest, TimingReport, expression, operandSizeBits, isSigned);

    /// <summary>
    /// Returns the amount of cycles required to perform the specified unary <paramref name="expression"/>.
    /// </summary>
    decimal GetClockCyclesNeededForUnaryOperation(UnaryOperatorExpression expression, int operandSizeBits, bool isSigned) =>
        DeviceDriverHelper.ComputeClockCyclesForUnaryOperation(DeviceManifest, TimingReport, expression, operandSizeBits, isSigned);

    /// <summary>
    /// Used during <see cref="MemoryConfiguration.Create"/> to set up device specific configuration.
    /// </summary>
    void ConfigureMemory(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration);
}
