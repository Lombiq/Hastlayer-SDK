using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Models;

namespace Hast.Layer.EmptyRepresentationFactories;

internal static class EmptyHardwareImplementationFactory
{
    public static IHardwareImplementation Create() => new HardwareImplementation();
}
