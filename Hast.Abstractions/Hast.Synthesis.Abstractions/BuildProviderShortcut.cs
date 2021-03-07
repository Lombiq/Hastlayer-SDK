using Hast.Layer;

namespace Hast.Synthesis.Abstractions
{
    public delegate bool BuildProviderShortcut(
        IHardwareImplementationCompositionContext context,
        IHardwareImplementation implementation);
}
