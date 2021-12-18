using Hast.Layer;

namespace Hast.Communication.Extensibility.Events
{
    /// <summary>
    /// The context for a hardware execution of a hardware-implemented member.
    /// </summary>
    public interface IMemberHardwareExecutionContext : IMemberInvocationContext
    {
        /// <summary>
        /// Gets debug and runtime information about the hardware execution.
        /// </summary>
        IHardwareExecutionInformation HardwareExecutionInformation { get; }
    }
}
