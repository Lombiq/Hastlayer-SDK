using Hast.Common.Interfaces;
using Hast.Layer;
using System.Threading.Tasks;

namespace Hast.Synthesis.Abstractions
{
    /// <summary>
    /// Service for composing the hardware implementation for a given hardware description, e.g. putting all the source
    /// files where they should be, running hardware synthesis, etc.
    /// </summary>
    public interface IHardwareImplementationComposer : IDependency
    {
        /// <summary>
        /// Indicates whether the hardware implementation composer can handle the given context.
        /// </summary>
        /// <param name="context">All the data necessary for the hardware implementation composition to happen.</param>
        bool CanCompose(IHardwareImplementationCompositionContext context);

        /// <summary>
        /// Composes the hardware implementation for the given hardware representation.
        /// </summary>
        /// <param name="context">All the data necessary for the hardware implementation composition to happen.</param>
        /// <returns>The handle to the synthesized hardware implementation.</returns>
        Task<IHardwareImplementation> ComposeAsync(IHardwareImplementationCompositionContext context);
    }
}
