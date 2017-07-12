using System.Collections.Generic;
using System.Reflection;

namespace Hast.Layer
{
    public class HastlayerConfiguration : IHastlayerConfiguration
    {
        private static readonly HastlayerConfiguration _default = new HastlayerConfiguration();
        public static HastlayerConfiguration Default { get { return _default; } }

        /// <summary>
        /// Extensions that can provide implementations for Hastlayer services or hook into the hardware generation 
        /// pipeline. These should be Orchard extensions.
        /// </summary>
        public IEnumerable<Assembly> Extensions { get; set; }

        /// <summary>
        /// The usage flavor of Hastlayer for different scenarios. Defaults to <see cref="HastlayerFlavor.Developer"/>.
        /// </summary>
        public HastlayerFlavor Flavor { get; set; } = HastlayerFlavor.Developer;


        public HastlayerConfiguration()
        {
            Extensions = new List<Assembly>();
        }

        public HastlayerConfiguration(IHastlayerConfiguration previousConfiguration)
        {
            Extensions = previousConfiguration.Extensions;
            Flavor = previousConfiguration.Flavor;
        }
    }
}
