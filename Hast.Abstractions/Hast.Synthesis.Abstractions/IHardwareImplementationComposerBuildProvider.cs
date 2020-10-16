﻿using Hast.Common.Interfaces;
using Hast.Layer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationComposerBuildProvider : IDependency
    {
        IEnumerable<string> SupportedComposers { get; }

        bool IsSupported(IHardwareImplementationCompositionContext context, IHardwareImplementation implementation);

        Task BuildAsync(IHardwareImplementationCompositionContext context, IHardwareImplementation implementation);
    }
}
