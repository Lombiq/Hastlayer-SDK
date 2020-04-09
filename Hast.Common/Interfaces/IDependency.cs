using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hast.Common.Interfaces
{
    /// <summary>
    /// Base interface for services that are instantiated per unit of work (i.e. web request).
    /// </summary>
    public interface IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per shell/tenant.
    /// </summary>
    public interface ISingletonDependency : IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per usage.
    /// </summary>
    public interface ITransientDependency : IDependency
    {
    }

    /// <summary>
    /// Indicates that the <see cref="IDependency"/> has its own initializer which should be invoked right before the
    /// service is added to the <see cref="IServiceCollection"/>.
    /// </summary>
    public class IDependencyInitializerAttribute : Attribute
    {
        /// <summary>
        /// The name of the public static method which will be invoked.
        /// </summary>
        public string MemberName { get; }

        /// <param name="memberName">
        /// The name of a public static method that takes one <see cref="IServiceCollection" /> argument and returns void.
        /// </param>
        public IDependencyInitializerAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
