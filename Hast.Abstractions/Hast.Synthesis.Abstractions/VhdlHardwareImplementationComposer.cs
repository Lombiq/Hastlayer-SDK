using Hast.Common.Extensions;
using Hast.Common.Models;
using Hast.Layer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public class VhdlHardwareImplementationComposer : IHardwareImplementationComposer
    {
        private readonly IEnumerable<IHardwareImplementationComposerBuildProvider> _buildProviders;


        public VhdlHardwareImplementationComposer(
            IEnumerable<IHardwareImplementationComposerBuildProvider> buildProviders) =>
            _buildProviders = buildProviders;


        public bool CanCompose(IHardwareImplementationCompositionContext context) =>
            context.HardwareDescription is VhdlHardwareDescription;

        public async Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context)
        {
            var implementation = new HardwareImplementation();

            var buildProviders = _buildProviders
                .Where(provider => provider.CanCompose(context))
                .OrderByRequirements<IHardwareImplementationComposerBuildProvider, string>();
            foreach (var buildProvider in buildProviders) await buildProvider.BuildAsync(context, implementation);

            return implementation;
        }
    }
}
