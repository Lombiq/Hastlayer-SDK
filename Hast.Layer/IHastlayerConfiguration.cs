using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer
{
    /// <summary>
    /// The usage flavor of Hastlayer for different scenarios.
    /// </summary>
    public enum HastlayerFlavor
    {
        /// <summary>
        /// Flavor for the developers of Hastlayer itself. It includes the full source code.
        /// </summary>
        Developer,

        /// <summary>
        /// Flavor for end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service.
        /// </summary>
        Client
    }


    public interface IHastlayerConfiguration : ISingletonDependency
    {
        /// <summary>
        /// Invoked before the <see cref="IServiceProvider"/> is built.
        /// </summary>
        Action<IHastlayerConfiguration, IServiceCollection> OnServiceRegistration { get; }

        /// <summary>
        /// Extensions that can provide implementations for Hastlayer services or hook into the hardware generation 
        /// pipeline. These should be Orchard extensions.
        /// </summary>
        IEnumerable<Assembly> Extensions { get; }

        /// <summary>
        /// The usage flavor of Hastlayer for different scenarios.
        /// </summary>
        HastlayerFlavor Flavor { get; }

        /// <summary>
        /// The collection of assemblies to be dynamically loaded. If the item is a directory, then the DLL files in
        /// that directory are loaded instead.
        /// </summary>
        IEnumerable<string> DynamicAssemblies { get; }

        /// <summary>
        /// The location of the App_Data folder
        /// </summary>
        string AppDataFolderPath { get; }


        void InvokeOnServiceRegistration(IServiceCollection services);
    }
}
