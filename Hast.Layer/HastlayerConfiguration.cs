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

        /// <inheritdoc/>
        public HastlayerFlavor Flavor { get; set; } = HastlayerFlavor.Developer;

        /// <inheritdoc/>
        public string AppDataFolderPath { get; set; } = null;
    }
}
