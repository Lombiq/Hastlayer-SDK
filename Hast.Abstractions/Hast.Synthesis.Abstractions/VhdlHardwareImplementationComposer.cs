using Hast.Common.Extensions;
using Hast.Common.Models;
using Hast.Layer;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public class VhdlHardwareImplementationComposer : IHardwareImplementationComposer
    {
        private readonly IEnumerable<IHardwareImplementationComposerBuildProvider> _buildProviders;
        private readonly ILogger<VhdlHardwareImplementationComposer> _logger;

        public VhdlHardwareImplementationComposer(
            IEnumerable<IHardwareImplementationComposerBuildProvider> buildProviders,
            ILogger<VhdlHardwareImplementationComposer> logger)
        {
            _buildProviders = buildProviders;
            _logger = logger;
        }

        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription;

        public async Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context)
        {
            var implementation = new HardwareImplementation();

            var buildProviders = _buildProviders
                .Where(provider => provider.CanCompose(context))
                .OrderByRequirements<IHardwareImplementationComposerBuildProvider, string>();

            foreach (var buildProvider in buildProviders) buildProvider.AddShortcutsToOtherProviders(buildProviders);

            foreach (var buildProvider in buildProviders)
            {
                var stop = buildProvider
                    .Shortcuts
                    .Select(pair => new { pair.Key, pair.Value }) // Make it nullable.
                    .FirstOrDefault(item => item.Value(context));

                if (stop != null)
                {
                    _logger.LogInformation(
                        "The {0} provider asked to skip the {1} provider.",
                        stop.Key,
                        buildProvider.Name);
                }

                await buildProvider.BuildAsync(context, implementation);
            }

            return implementation;
        }
    }
}
