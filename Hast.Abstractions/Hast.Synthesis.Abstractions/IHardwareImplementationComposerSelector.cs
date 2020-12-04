using Hast.Common.Interfaces;

namespace Hast.Synthesis.Abstractions
{
    /// <summary>
    /// Service for selecting an <see cref="IHardwareImplementationComposer"/> instance based on the given hardware
    /// implementation composition context.
    /// </summary>
    public interface IHardwareImplementationComposerSelector : IDependency
    {
        /// <summary>
        /// Retrieves the matching <see cref="IHardwareImplementationComposer"/> instance for the given hardware
        /// implementation context, if any.
        /// </summary>
        /// <param name="context">All the data necessary for the hardware implementation composition to happen.</param>
        /// <returns>
        /// The matching <see cref="IHardwareImplementationComposer"/> instance if one is found, <c>null</c> otherwise.
        /// </returns>
        IHardwareImplementationComposer GetHardwareImplementationComposer(IHardwareImplementationCompositionContext context);
    }
}
