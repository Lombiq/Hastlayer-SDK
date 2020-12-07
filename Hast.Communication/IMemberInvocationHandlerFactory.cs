using Castle.DynamicProxy;
using Hast.Common.Interfaces;
using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;
using Hast.Layer;
using System;

namespace Hast.Communication
{
    /// <summary>
    /// Delegate for handling member invocations of objects whose logic is implemented as hardware.
    /// </summary>
    /// <param name="invocation">The context of the member invocation.</param>
    public delegate void MemberInvocationHandler(IInvocation invocation);

    /// <summary>
    /// Creates delegates that will handle member invocations issued to members of objects whose logic is implemented
    /// as hardware.
    /// </summary>
    public interface IMemberInvocationHandlerFactory : ISingletonDependency
    {
#pragma warning disable S3906 // Event Handlers should have the correct signature

        /// <summary>
        /// Event that fires once the hardware execution has concluded.
        /// </summary>
        event EventHandler<IMemberHardwareExecutionContext> MemberExecutedOnHardware;

        /// <summary>
        /// Event that fires before the hardware execution starts.
        /// </summary>
        event EventHandler<IMemberInvocationContext> MemberInvoking;
#pragma warning restore S3906 // Event Handlers should have the correct signature

        /// <summary>
        /// Creates a new instance of <see cref="MemberInvocationHandler"/> from the generated hardware representation.
        /// </summary>
        /// <param name="hardwareRepresentation">The result of <c>IHastlayer.GenerateHardwareAsync</c>.</param>
        /// <param name="target">The object to be proxied.</param>
        /// <param name="configuration">Configuration for <c>IHastlayer.GenerateProxyAsync</c>.</param>
        MemberInvocationHandler CreateMemberInvocationHandler(IHardwareRepresentation hardwareRepresentation, object target, IProxyGenerationConfiguration configuration);
    }
}
