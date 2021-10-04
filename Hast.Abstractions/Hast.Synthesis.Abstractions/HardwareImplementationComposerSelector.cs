using Hast.Common.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hast.Synthesis.Abstractions
{
    public class HardwareImplementationComposerSelector : IHardwareImplementationComposerSelector
    {
        private readonly IEnumerable<IHardwareImplementationComposer> _hardwareImplementationComposers;


        public HardwareImplementationComposerSelector(IEnumerable<IHardwareImplementationComposer> hardwareImplementationComposers)
        {
            _hardwareImplementationComposers = hardwareImplementationComposers;
        }


        public IHardwareImplementationComposer GetHardwareImplementationComposer(IHardwareImplementationCompositionContext context)
        {
            Argument.ThrowIfNull(context, nameof(context));
            return _hardwareImplementationComposers.Where(composer => composer.CanCompose(context)).FirstOrDefault();
        }
    }
}
