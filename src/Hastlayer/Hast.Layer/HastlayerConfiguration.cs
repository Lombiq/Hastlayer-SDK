using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer;

public class HastlayerConfiguration : IHastlayerConfiguration
{
    public static IHastlayerConfiguration Default { get; } = new HastlayerConfiguration();

    /// <inheritdoc/>
    public Action<IHastlayerConfiguration, IServiceCollection> OnServiceRegistration { get; set; }

    /// <inheritdoc/>
    public IEnumerable<Assembly> Extensions { get; set; } = new List<Assembly>();

    /// <inheritdoc/>
    public string AppDataFolderPath { get; set; }

    /// <inheritdoc/>
    public Action<ILoggingBuilder> ConfigureLogging { get; set; }
}
