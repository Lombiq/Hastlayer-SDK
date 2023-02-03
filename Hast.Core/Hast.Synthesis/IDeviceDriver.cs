using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Helpers;
using Hast.Synthesis.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Synthesis;

/// <summary>
/// Provides FPGA-specific implementations.
/// </summary>
/// <remarks>
/// <para>
/// Separated the manifest-providing part into <see cref="IDeviceManifestProvider"/> so that can be available in the
/// Client flavor too but keeping the rest of the driver implementation only part of Hast.Core.
/// </para>
/// </remarks>
public interface IDeviceDriver : IDeviceManifestProvider
{
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
}
