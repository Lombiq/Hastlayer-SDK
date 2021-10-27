using Hast.Transformer.Abstractions;
using System.Threading.Tasks;

namespace Hast.Layer.Extensibility.Events
{
    /// <summary>
    /// Events to be executed before and after transformation inside <see cref="Hastlayer.GenerateHardware"/>.
    /// </summary>
    public interface ITransformationEvents
    {
        /// <summary>
        /// Executes before <see cref="ITransformer.Transform"/> may be called.
        /// </summary>
        Task BeforeTransformAsync() => Task.CompletedTask;

        /// <summary>
        /// Executes after <see cref="ITransformer.Transform"/> may be called.
        /// </summary>
        Task AfterTransformAsync(IHardwareDescription hardwareDescription) => Task.CompletedTask;
    }
}
