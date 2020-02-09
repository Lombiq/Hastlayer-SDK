using Hast.Common.Interfaces;

namespace Hast.Communication.Extensibility.Events
{
    /// <summary>
    /// Event handler to hook into the of invoking hardware-implemented members.
    /// </summary> 
    public interface IMemberInvocationEventHandler : IEventHandler
    {
        /// <summary>
        /// Fired when a hardware-implemented member is being invoked.
        /// </summary>
        /// <param name="invocationContext">The context of the member invocation.</param>
        void MemberInvoking(IMemberInvocationContext invocationContext);

        /// <summary>
        /// Fired when a hardware-implemented member finished being executed as hardware-implemented logic.
        /// </summary>
        /// <param name="hardwareExecutionContext">The context of the hardware execution.</param>
        void MemberExecutedOnHardware(IMemberHardwareExecutionContext hardwareExecutionContext);
    }
}
