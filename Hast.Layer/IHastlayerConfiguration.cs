using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer;

/// <summary>
/// Provides settings for <see cref="IHastlayer"/> initialization.
/// </summary>
public interface IHastlayerConfiguration : IHastlayerFlavorProvider
{
    /// <summary>
    /// Gets the action to be invoked before the <see cref="IServiceProvider"/> for Hastlayer's dependency
    /// injection is built. Add single service registrations or other service collection customizations here.
    /// </summary>
    Action<IHastlayerConfiguration, IServiceCollection> OnServiceRegistration { get; }

    /// <summary>
    /// Gets the extensions that can provide implementations for Hastlayer services or hook into the hardware generation
    /// pipeline. These should include services that implement <see cref="IDependency"/> directly or indirectly.
    /// </summary>
    IEnumerable<Assembly> Extensions { get; }

    /// <summary>
    /// Gets the location of the App_Data folder.
    /// </summary>
    string AppDataFolderPath { get; }

    /// <summary>
    /// Gets the extension points for customizing the logger.
    /// </summary>
    Action<ILoggingBuilder> ConfigureLogging { get; }
}
