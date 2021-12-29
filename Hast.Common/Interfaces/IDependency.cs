using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Common.Interfaces
{
    /// <summary>
    /// Base interface for services that are instantiated per unit of work (i.e. web request).
    /// </summary>
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Necessary for the automatic dependency registration.")]
    public interface IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per shell/tenant.
    /// </summary>
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Necessary for the automatic dependency registration.")]
    public interface ISingletonDependency : IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per usage.
    /// </summary>
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Necessary for the automatic dependency registration.")]
    public interface ITransientDependency : IDependency
    {
    }

    /// <summary>
    /// Indicates that the <see cref="IDependency"/> has its own initializer which should be invoked right before the
    /// service is added to the <see cref="IServiceCollection"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DependencyInitializerAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the public static method which will be invoked.
        /// </summary>
        public string MemberName { get; }

        /// <param name="memberName">
        /// The name of a public static method that takes one <see cref="IServiceCollection" /> argument and returns void.
        /// </param>
        public DependencyInitializerAttribute(string memberName) => MemberName = memberName;
    }
}
