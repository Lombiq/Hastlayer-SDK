using Hast.Common.Interfaces;
using Hast.Layer;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationComposerBuildProvider : IDependency
    {
        string Name { get; }

        Task<IHardwareImplementation> BuildAsync(IHardwareImplementationCompositionContext context, string buildTarget);
    }
}
