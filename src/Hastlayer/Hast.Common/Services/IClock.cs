using Hast.Common.Interfaces;
using System;

namespace Hast.Common.Services;

/// <summary>
/// UTC time service.
/// </summary>
#pragma warning disable MA0188 // This custom abstraction pre-dates System.TimeProvider and is kept for compatibility.
public interface IClock : ISingletonDependency
#pragma warning restore MA0188
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    DateTime UtcNow { get; }
}
