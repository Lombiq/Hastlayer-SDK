using Hast.Common.Interfaces;

namespace Hast.Layer
{
    /// <summary>
    /// Provides a place to store the <see cref="IHardwareGenerationConfiguration"/> for dependency injection.
    /// </summary>
    public interface IHardwareGenerationConfigurationAccessor : IDependency
    {
        /// <summary>
        /// Gets or sets the configuration to be set by <c>IHastlayer.GenerateHardware()</c>.
        /// </summary>
        IHardwareGenerationConfiguration Value { get; set; }
    }
}
