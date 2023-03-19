using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Services;
using Hast.Synthesis.Helpers;
using Hast.Synthesis.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Synthesis;

/// <summary>
/// Provides FPGA-specific implementations.
/// </summary>
/// <remarks>
/// <para>
/// This was originally separated from the rest of <see cref="IDeviceManifestProvider"/> so that can be available in the
/// Client flavor, without Hast.Core. Now, after the open-sourcing, we can consider simplifying it, see
/// <see href="https://github.com/Lombiq/Hastlayer-SDK/issues/99"/>.
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
