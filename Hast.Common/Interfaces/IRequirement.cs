using System.Collections.Generic;

namespace Hast.Common.Interfaces;

/// <summary>
/// Represents a type which has requirements of the same type. For example service implementation that expects certain
/// other implementations of the same service to be run before it.
/// </summary>
/// <typeparam name="T">The type of the key, typically <see cref="string"/>.</typeparam>
public interface IRequirement<T>
{
    /// <summary>
    /// Gets the identifier other implementations may use to refer to this type.
    /// </summary>
    T Name => typeof(T) == typeof(string) ? (T)(object)GetType().Name : default;

    /// <summary>
    /// Gets the list of requirements this type depends on.
    /// </summary>
    ISet<T> Requirements => new HashSet<T>();
}
