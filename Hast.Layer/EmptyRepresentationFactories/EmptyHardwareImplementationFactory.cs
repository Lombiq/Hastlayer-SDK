using Hast.Synthesis.Models;

namespace Hast.Layer.EmptyRepresentationFactories;

internal static class EmptyHardwareImplementationFactory
{
    public static IHardwareImplementation Create() => new HardwareImplementation();
}
