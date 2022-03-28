using System;

namespace Hast.Layer;

/// <summary>
/// Carries debug and runtime information about a hardware execution.
/// </summary>
public interface IHardwareExecutionInformation
{
    /// <summary>
    /// Gets the net (without the communication round-trip) execution time on the hardware, in milliseconds.
    /// </summary>
    decimal HardwareExecutionTimeMilliseconds { get; }

    /// <summary>
    /// Gets the full (including the communication round-trip) execution time of the hardware execution, in
    /// milliseconds.
    /// </summary>
    decimal FullExecutionTimeMilliseconds { get; }

    /// <summary>
    /// Gets the date and time when the execution started.
    /// </summary>
    DateTime StartedUtc { get; }
}
