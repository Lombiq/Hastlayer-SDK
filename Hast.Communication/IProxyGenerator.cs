using Hast.Common.Interfaces;
using Hast.Layer;
using static Hast.Communication.ProxyGenerator;

namespace Hast.Communication
{
    /// <summary>
    /// Generates proxies for objects whose logic is implemented as hardware to redirect member calls to the hardware
    /// implementation.
    /// </summary>
    public interface IProxyGenerator : ISingletonDependency
    {
        /// <summary>
        /// Returns a proxied version of <paramref name="target"/> via <see cref="MemberInvocationInterceptor"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to proxy.</typeparam>
        T CreateCommunicationProxy<T>(IHardwareRepresentation hardwareRepresentation, T target, IProxyGenerationConfiguration configuration)
            where T : class;
    }
}
