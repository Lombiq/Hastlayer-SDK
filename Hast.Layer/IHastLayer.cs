using Hast.Communication.Services;
using Hast.Layer.Extensibility.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Layer
{
    public interface IHastlayer : IDisposable
    {
        /// <summary>
        /// Occurs when the member invocation (e.g. a method call) was transferred to hardware and finished there.
        /// </summary>
        event ExecutedOnHardwareEventHandler ExecutedOnHardware;

        /// <summary>
        /// Gets those devices which have their support drivers loaded.
        /// </summary>
        /// <returns>Those devices which have their support drivers loaded.</returns>
        Task<IEnumerable<IDeviceManifest>> GetSupportedDevices();

        /// <summary>
        /// Generates and implements a hardware representation of the given assemblies. Be sure to use assemblies built
        /// with the Debug configuration.
        /// </summary>
        /// <param name="assemblyPaths">The assemblies' paths that should be implemented as hardware.</param>
        /// <param name="configuration">Configuration for how the hardware generation should happen.</param>
        /// <returns>The representation of the assemblies implemented as hardware.</returns>
        /// <exception cref="HastlayerException">
        /// Thrown if any lower-level exception or other error happens during hardware generation.
        /// </exception>
        Task<IHardwareRepresentation> GenerateHardware(
            IEnumerable<string> assemblyPaths,
            IHardwareGenerationConfiguration configuration);

        /// <summary>
        /// Generates a proxy for the given object that will transfer suitable calls to the hardware implementation.
        /// </summary>
        /// <typeparam name="T">Type of the object to generate a proxy for.</typeparam>
        /// <param name="hardwareRepresentation">The representation of the assemblies implemented as hardware.</param>
        /// <param name="hardwareObject">The object to generate the proxy for.</param>
        /// <param name="configuration">Configuration for how the proxy generation should happen.</param>
        /// <returns>The generated proxy object.</returns>
        /// <exception cref="HastlayerException">
        /// Thrown if any lower-level exception or other error happens during proxy generation.
        /// </exception>
        Task<T> GenerateProxy<T>(
            IHardwareRepresentation hardwareRepresentation,
            T hardwareObject,
            IProxyGenerationConfiguration configuration) where T : class;

        /// <summary>
        /// Gets the <see cref="ICommunicationService"/> based on the channel name.
        /// </summary>
        /// <param name="communicationChannelName">The <see cref="ICommunicationService.ChannelName"/> value.</param>
        /// <returns>The matching communication service.</returns>
        Task<ICommunicationService> GetCommunicationService(string communicationChannelName);
    }
}
