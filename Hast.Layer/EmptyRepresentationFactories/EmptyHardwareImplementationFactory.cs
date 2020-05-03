using Hast.Synthesis.Abstractions;

namespace Hast.Layer.EmptyRepresentationFactories
{
    internal static class EmptyHardwareImplementationFactory
    {
        public static IHardwareImplementation Create() => new HardwareImplementation();
    }
}
