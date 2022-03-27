using Hast.Common.Interfaces;
using Hast.Layer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions;

/// <summary>
/// If <see cref="CanCompose"/> returns <see langword="true"/> it performs any build actions and fills in the <see
/// cref="IHardwareImplementation"/> given by the <see cref="VhdlHardwareImplementationComposer"/>.
/// </summary>
public interface IHardwareImplementationComposerBuildProvider : IRequirement<string>, IProgressInvoker, IDependency
{
    /// <summary>
    /// Gets the functions installed by other providers. If any of them returns <see langword="true"/> this provider is
    /// skipped.
    /// </summary>
    IDictionary<string, BuildProviderShortcut> Shortcuts { get; }

    /// <summary>
    /// Determines if the instance is applicable to the current composition task based on the <paramref
    /// name="context"/>.
    /// </summary>
    bool CanCompose(IHardwareImplementationCompositionContext context);

    /// <summary>
    /// Performs the building and sets up the <paramref name="implementation"/>.
    /// </summary>
    Task BuildAsync(IHardwareImplementationCompositionContext context, IHardwareImplementation implementation);

    /// <summary>
    /// If implemented, it adds <see cref="BuildProviderShortcut"/> to the <see cref="Shortcuts"/> of other <see
    /// cref="IHardwareImplementationComposerBuildProvider"/> instances.
    /// </summary>
    void AddShortcuts(IEnumerable<IHardwareImplementationComposerBuildProvider> providers)
    {
        // The default behavior is to do nothing.
    }

    /// <summary>
    /// If implemented, it performs cleanup tasks and removes temporary resources. This is deferred until all build
    /// providers have finished.
    /// </summary>
    Task CleanupAsync(IHardwareImplementationCompositionContext context) => Task.CompletedTask;
}

/// <summary>
/// Exposes the Progress event.
/// </summary>
public interface IProgressInvoker
{
    /// <summary>
    /// Invokes the progress event, if any.
    /// </summary>
    void InvokeProgress(BuildProgressEventArgs eventArgs);
}
