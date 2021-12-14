using Hast.Common.Extensibility.Pipeline;

namespace Hast.Communication.Extensibility.Pipeline
{
    /// <summary>
    /// Pipeline step to change the invocation of hardware-implemented members.
    /// </summary>
    public interface IMemberInvocationPipelineStep : IPipelineStep
    {
        /// <summary>
        /// Determines whether the invocation of the hardware-implemented member can continue as hardware-implemented
        /// logic. You can use this to decide in runtime whether a member invocation should continue on hardware.
        /// </summary>
        /// <param name="invocationContext">The context of the member invocation.</param>
        /// <returns><c>true</c> if the execution can continue on hardware, <c>false</c> otherwise.</returns>
        bool CanContinueHardwareExecution(IMemberInvocationPipelineStepContext invocationContext);
    }
}
