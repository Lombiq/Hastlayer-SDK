namespace Hast.Synthesis.Abstractions;

/// <summary>
/// If it returns <see langword="true"/> the associated build provider should be skipped.
/// </summary>
/// <param name="context">
/// The current composition context that would be passed to the <see
/// cref="IHardwareImplementationComposerBuildProvider"/>.
/// </param>
public delegate bool BuildProviderShortcut(IHardwareImplementationCompositionContext context);
