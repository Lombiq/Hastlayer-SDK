using Hast.Common.Interfaces;

namespace Hast.Common.Services
{
    /// <summary>
    /// Service for serialization and deserialization of JSON strings.
    /// </summary>
    public interface IJsonConverter : ISingletonDependency
    {
        /// <summary>
        /// Converts an object into JSON serialized string.
        /// </summary>
        string Serialize(object source);

        /// <summary>
        /// Converts a JSON serialized string into an object of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the resut.</typeparam>
        T Deserialize<T>(string json);
    }
}
