using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer
{
    public class HastlayerConfiguration : IHastlayerConfiguration
    {
        public static IHastlayerConfiguration Default { get; } = new HastlayerConfiguration();

        /// <inheritdoc/>
        public Action<IHastlayerConfiguration, IServiceCollection> OnServiceRegistration { get; set; }

        /// <inheritdoc/>
        public IEnumerable<Assembly> Extensions { get; set; } = new List<Assembly>();

        /// <summary>
        /// The usage flavor of Hastlayer for different scenarios. Defaults to <see cref="HastlayerFlavor.Developer"/>.
        /// </summary>
        public HastlayerFlavor Flavor { get; set; } = HastlayerFlavor.Developer;

        /// <inheritdoc/>
        public IEnumerable<string> DynamicAssemblies { get; set; } = Array.Empty<string>();

        /// <inheritdoc/>
        public string AppDataFolderPath { get; set; } = "Hastlayer/App_Data";
    }
}
