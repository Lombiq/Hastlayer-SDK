namespace Hast.Layer;

/// <summary>
/// Carries debug information about the software execution of hardware-executed members in case the hardware execution
/// was canceled or verified in software.
/// </summary>
public interface ISoftwareExecutionInformation
{
    /// <summary>
    /// Gets the execution time in software, in milliseconds.
    /// </summary>
    decimal SoftwareExecutionTimeMilliseconds { get; }
}
