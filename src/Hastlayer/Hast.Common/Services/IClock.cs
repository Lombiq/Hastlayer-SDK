using Hast.Common.Interfaces;
using System;

namespace Hast.Common.Services;

/// <summary>
/// UTC time service.
/// </summary>
public interface IClock : ISingletonDependency
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    DateTime UtcNow { get; }
}
