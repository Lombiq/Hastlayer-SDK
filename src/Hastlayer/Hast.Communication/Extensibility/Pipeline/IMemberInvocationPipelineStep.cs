using Hast.Common.Extensibility.Pipeline;

namespace Hast.Communication.Extensibility.Pipeline;

/// <summary>
/// Pipeline step to change the invocation of hardware-implemented members.
/// </summary>
public interface IMemberInvocationPipelineStep : IPipelineStep
{
    /// <summary>
    /// Determines whether the invocation of the hardware-implemented member can continue as hardware-implemented logic.
    /// You can use this to decide in runtime whether a member invocation should continue on hardware.
    /// </summary>
    /// <param name="invocationContext">The context of the member invocation.</param>
    /// <returns>
    /// <see langword="true"/> if the execution can continue on hardware, <see langword="false"/> otherwise.
    /// </returns>
    bool CanContinueHardwareExecution(IMemberInvocationPipelineStepContext invocationContext);
}
