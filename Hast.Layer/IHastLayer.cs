using Hast.Common.Services;
using Hast.Communication.Services;
using Hast.Layer.Extensibility.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Layer
{
    public interface IHastlayer : IDisposable
    {
        /// <summary>
        /// Occurs when the member invocation (e.g. a method call) was transferred to hardware and finished there.
        /// </summary>
        event ExecutedOnHardwareEventHandler ExecutedOnHardware;
        event InvokingEventHandler Invoking;

        /// <summary>
        /// Gets those devices which have their support drivers loaded.
        /// </summary>
        /// <returns>Those devices which have their support drivers loaded.</returns>
        IEnumerable<IDeviceManifest> GetSupportedDevices();

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
        DisposableContainer<ICommunicationService> GetCommunicationService(string communicationChannelName);

        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on
        /// the FPGA for transformed algorithms.
        /// </summary>
        /// <param name="manifestProvider">The manifest provider or driver for the device to be used.</param>
        /// <param name="cellCount">
        /// The number of memory "cells". The memory is divided into independently accessible "cells"; the size of the
        /// allocated memory space is calculated from the cell count and the cell size indicated in
        /// <see cref="SimpleMemory.MemoryCellSizeBytes"/>.
        /// </param>
        /// <returns>A new instance of <see cref="SimpleMemory"/></returns>
        SimpleMemory CreateMemory(IDeviceManifestProvider manifestProvider, int cellCount);

        /// <summary>
        /// Constructs a new <see cref="SimpleMemory"/> object that represents a simplified memory model available on
        /// the FPGA for transformed algorithms.
        /// </summary>
        /// <param name="manifestProvider">The manifest provider or driver for the device to be used.</param>
        /// <param name="data">
        /// The input data which is referenced by or copied into the <see cref="SimpleMemory"/> depending on the
        /// selected device's characteristics, particularly its <see cref="MemoryConfiguration{T}.MinimumPrefix"/>.
        /// </param>
        /// <param name="withPrefixCells">The amount of empty header space reserved in the <see cref="data"/>.</param>
        /// <returns>A new instance of <see cref="SimpleMemory"/></returns>
        SimpleMemory CreateMemory(IDeviceManifestProvider manifestProvider, Memory<byte> data, int withPrefixCells = 0);
    }
}
