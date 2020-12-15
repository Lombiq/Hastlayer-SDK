using Castle.DynamicProxy;
using Hast.Layer;

namespace Hast.Communication.Extensibility
{
    /// <summary>
    /// The context of the invocation of a hardware-implemented member.
    /// </summary>
    public interface IMemberInvocationContext
    {
        /// <summary>
        /// Gets the context of the member invocation.
        /// </summary>
        IInvocation Invocation { get; }

        /// <summary>
        /// Gets the full name of the invoked member, including the full namespace of the parent type(s) as well as their
        /// return type and the types of their (type) arguments.
        /// </summary>
        string MemberFullName { get; }

        /// <summary>
        /// Gets the materialized hardware behind the hardware-implemented members.
        /// </summary>
        IHardwareRepresentation HardwareRepresentation { get; }

        /// <summary>
        /// Debug information about the software execution of hardware-executed members in case the hardware execution
        /// was canceled or verified in software.
        /// </summary>
        ISoftwareExecutionInformation SoftwareExecutionInformation { get; }
    }
}
