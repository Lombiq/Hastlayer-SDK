using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Layer;

public static class HastlayerExtensions
{
    /// <summary>
    /// Generates a proxy for the given object that will transfer suitable calls to the hardware implementation using
    /// the default proxy generation configuration.
    /// </summary>
    /// <typeparam name="T">Type of the object to generate a proxy for.</typeparam>
    /// <param name="hardwareRepresentation">The representation of the assemblies implemented as hardware.</param>
    /// <param name="hardwareObject">The object to generate the proxy for.</param>
    /// <returns>The generated proxy object.</returns>
    /// <exception cref="HastlayerException">
    /// Thrown if any lower-level exception or other error happens during proxy generation.
    /// </exception>
    public static Task<T> GenerateProxyAsync<T>(
        this IHastlayer hastlayer,
        IHardwareRepresentation hardwareRepresentation,
        T hardwareObject)
        where T : class =>
        hastlayer.GenerateProxyAsync(hardwareRepresentation, hardwareObject, ProxyGenerationConfiguration.Default);

    /// <summary>
    /// Generates and implements a hardware representation of the given assemblies. Be sure to use assemblies built with
    /// the Debug configuration.
    /// </summary>
    /// <param name="assemblies">The assemblies that should be implemented as hardware.</param>
    /// <param name="configuration">Configuration for how the hardware generation should happen.</param>
    /// <returns>The representation of the assemblies implemented as hardware.</returns>
    /// <exception cref="HastlayerException">
    /// Thrown if any lower-level exception or other error happens during hardware generation.
    /// </exception>
    public static Task<IHardwareRepresentation> GenerateHardwareAsync(
        this IHastlayer hastlayer,
        IEnumerable<Assembly> assemblies,
        IHardwareGenerationConfiguration configuration) =>
        hastlayer.GenerateHardwareAsync(assemblies.Select(assembly => assembly.Location), configuration);
}
