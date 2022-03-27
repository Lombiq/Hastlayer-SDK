namespace Hast.Communication.Extensibility.Pipeline;

/// <summary>
/// The context of the invocation of a hardware-implemented member in the pipeline.
/// </summary>
public interface IMemberInvocationPipelineStepContext : IMemberInvocationContext
{
    /// <summary>
    /// Gets a value indicating whether running the logic on hardware was canceled to resume member invocation in
    /// software.
    /// </summary>
    bool HardwareExecutionIsCancelled { get; }
}
