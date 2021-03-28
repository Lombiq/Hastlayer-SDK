using Hast.Common.Enums;
using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer
{
    public interface IHastlayerConfiguration : ISingletonDependency
    {
        /// <summary>
        /// Invoked before the <see cref="IServiceProvider"/> for Hastlayer's dependency injection is built. Add single
        /// service registrations or other service collection customizations here.
        /// </summary>
        Action<IHastlayerConfiguration, IServiceCollection> OnServiceRegistration { get; }

        /// <summary>
        /// Extensions that can provide implementations for Hastlayer services or hook into the hardware generation
        /// pipeline. These should include services that implement <see cref="IDependency"/> directly or indirectly.
        /// </summary>
        IEnumerable<Assembly> Extensions { get; }

        /// <summary>
        /// The usage flavor of Hastlayer for different scenarios. Defaults to <see cref="HastlayerFlavor.Client"/> in
        /// the client SDK and <see cref="HastlayerFlavor.Developer"/> otherwise.
        /// </summary>
        HastlayerFlavor Flavor { get; }

        /// <summary>
        /// The location of the App_Data folder
        /// </summary>
        string AppDataFolderPath { get; }

        /// <summary>
        /// Extension points for customizing the logger.
        /// </summary>
        Action<ILoggingBuilder> ConfigureLogging { get; }
    }
}
