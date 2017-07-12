using Castle.DynamicProxy;
using Hast.Layer;
using Orchard;

namespace Hast.Communication
{
    /// <summary>
    /// Delegate for handling member invocations of objects whose logic is implemented as hardware.
    /// </summary>
    /// <param name="invocation">The context of the member invocation.</param>
    /// <returns>
    /// <c>True</c> if the member invocation was successfully transferred to the hardware implementation, <c>false</c> 
    /// otherwise.
    /// </returns>
    public delegate void MemberInvocationHandler(IInvocation invocation);


    /// <summary>
    /// Creates delegates that will handle member invocations issued to members of objects whose logic is implemented 
    /// as hardware.
    /// </summary>
    public interface IMemberInvocationHandlerFactory : ISingletonDependency
    {
        MemberInvocationHandler CreateMemberInvocationHandler(IHardwareRepresentation hardwareRepresentation, object target, IProxyGenerationConfiguration configuration);
    }
}
