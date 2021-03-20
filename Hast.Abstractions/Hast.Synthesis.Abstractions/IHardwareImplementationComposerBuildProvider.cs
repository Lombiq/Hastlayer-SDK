using Hast.Common.Interfaces;
using Hast.Layer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    /// <summary>
    /// If <see cref="CanCompose"/> returns <see langword="true"/> it performs any build actions and fills in the
    /// <see cref="IHardwareImplementation"/> given by the <see cref="VhdlHardwareImplementationComposer"/>.
    /// </summary>
    public interface IHardwareImplementationComposerBuildProvider : IRequirement<string>, IDependency
    {
        /// <summary>
        /// Gets the functions installed by other providers. If any of them returns <see langword="true"/> this provider
        /// is skipped.
        /// </summary>
        Dictionary<string, BuildProviderShortcut> Shortcuts { get; }

        /// <summary>
        /// Determines if the instance is applicable to the current composition task based on the
        /// <paramref name="context"/>.
        /// </summary>
        bool CanCompose(IHardwareImplementationCompositionContext context);

        /// <summary>
        /// Performs the building and sets up the <paramref name="implementation"/>.
        /// </summary>
        Task BuildAsync(IHardwareImplementationCompositionContext context, IHardwareImplementation implementation);

        /// <summary>
        /// If implemented, it adds to the <see cref="Shortcuts"/> of other providers.
        /// </summary>
        void AddShortcutsToOtherProviders(IEnumerable<IHardwareImplementationComposerBuildProvider> providers) { }
    }
}
