using Hast.Common.Extensions;
using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions.Services;

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

        foreach (var buildProvider in buildProviders) buildProvider.AddShortcuts(buildProviders);

        foreach (var buildProvider in buildProviders)
        {
            var stop = buildProvider
                .Shortcuts
                .Select(pair => new { pair.Key, pair.Value }) // Make it nullable.
                .FirstOrDefault(item => item.Value(context))
                ?.Key;

            if (stop != null)
            {
                _logger.LogInformation(
                    "The {BuildProviderName} provider asked to skip the {OtherBuildProviderName} provider.",
                    stop,
                    buildProvider.Name);
                continue;
            }

            await buildProvider.BuildAsync(context, implementation);
        }

        // Cleanup is only executed once all build providers are done. This is necessary in case a provider needs the
        // temporary files of its dependency. This also means dependent build providers don't need to implement
        // CleanupAsync even if they have their own temporary files as long as they are in a directory that the
        // dependency cleans up anyway.
        foreach (var buildProvider in buildProviders) await buildProvider.CleanupAsync(context);

        return implementation;
    }
}
