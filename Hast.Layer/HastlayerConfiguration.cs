using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer
{
    public class HastlayerConfiguration : IHastlayerConfiguration
    {
        public static HastlayerConfiguration Default { get; } = new HastlayerConfiguration();

        /// <summary>
        /// Extensions that can provide implementations for Hastlayer services or hook into the hardware generation 
        /// pipeline. These should be Orchard extensions.
        /// </summary>
        public IEnumerable<Assembly> Extensions { get; set; } = new List<Assembly>();

        /// <summary>
        /// The usage flavor of Hastlayer for different scenarios. Defaults to <see cref="HastlayerFlavor.Developer"/>.
        /// </summary>
        public HastlayerFlavor Flavor { get; set; } = HastlayerFlavor.Developer;

        /// <summary>
        /// The collection of assemblies to be dynamically loaded. If the item is a directory, then the DLL files in
        /// that directory are loaded instead.
        /// </summary>
        public IEnumerable<string> DynamicAssemblies { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The service collection to be used as a basis of the <see cref="ServiceProvider"/> creation.
        /// </summary>
        public IServiceCollection BaseServiceCollection { get; set; } = null;

        /// <summary>
        /// The location of the App_Data folder
        /// </summary>
        public string AppDataFolderPath { get; set; } = "Hastlayer/App_Data";

        public static HastlayerConfiguration Clone(IHastlayerConfiguration previousConfiguration) =>
            new HastlayerConfiguration()
            {
                Extensions = previousConfiguration.Extensions,
                Flavor = previousConfiguration.Flavor,
                DynamicAssemblies = previousConfiguration.DynamicAssemblies,
                BaseServiceCollection = previousConfiguration.BaseServiceCollection,
            };

        public HastlayerConfiguration() { }

        /*
        public HastlayerConfiguration(IHastlayerConfiguration previousConfiguration)
        {
            Extensions = previousConfiguration.Extensions;
            Flavor = previousConfiguration.Flavor;
            DynamicAssemblies = previousConfiguration.DynamicAssemblies;
            BaseServiceCollection = previousConfiguration.BaseServiceCollection;
        } // */
    }
}
