using Hast.Common.Validation;
using Hast.Synthesis.Abstractions.Models;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Synthesis.Abstractions.Services;

public class HardwareImplementationComposerSelector : IHardwareImplementationComposerSelector
{
    private readonly IEnumerable<IHardwareImplementationComposer> _hardwareImplementationComposers;

    public HardwareImplementationComposerSelector(IEnumerable<IHardwareImplementationComposer> hardwareImplementationComposers) =>
        _hardwareImplementationComposers = hardwareImplementationComposers;

    public IHardwareImplementationComposer GetHardwareImplementationComposer(IHardwareImplementationCompositionContext context)
    {
        Argument.ThrowIfNull(context, nameof(context));
        return _hardwareImplementationComposers.FirstOrDefault(composer => composer.CanCompose(context));
    }
}
