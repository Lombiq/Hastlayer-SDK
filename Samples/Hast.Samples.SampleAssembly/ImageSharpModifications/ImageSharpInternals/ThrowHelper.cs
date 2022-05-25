// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications;

/// <summary>
/// Helper methods to throw exceptions.
/// </summary>
internal static class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> when <see cref="Guard.NotNull{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentNullExceptionForNotNull(string name) =>
        ThrowArgumentNullException(name, $"Parameter \"{name}\" must be not null.");

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when <see cref="Guard.NotNullOrWhiteSpace"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentExceptionForNotNullOrWhitespace(string value, string name)
    {
        if (value is null)
        {
            ThrowArgumentNullException(name, $"Parameter \"{name}\" must be not null.");
        }
        else
        {
            ThrowArgumentException(name, $"Parameter \"{name}\" must not be empty or whitespace.");
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Guard.MustBeLessThan{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeLessThan<T>(T value, T max, string name) =>
        ThrowArgumentOutOfRangeException(name, $"Parameter \"{name}\" ({typeof(T)}) must be less than {max}, was {value}");

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Guard.MustBeLessThanOrEqualTo{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeLessThanOrEqualTo<T>(T value, T maximum, string name) =>
        ThrowArgumentOutOfRangeException(name, $"Parameter \"{name}\" ({typeof(T)}) must be less than or equal to {maximum}, was {value}");

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Guard.MustBeGreaterThan{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeGreaterThan<T>(T value, T minimum, string name) =>
        ThrowArgumentOutOfRangeException(name, $"Parameter \"{name}\" ({typeof(T)}) must be greater than {minimum}, was {value}");

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Guard.MustBeGreaterThanOrEqualTo{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeGreaterThanOrEqualTo<T>(T value, T minimum, string name) =>
        ThrowArgumentOutOfRangeException(name, $"Parameter \"{name}\" ({typeof(T)}) must be greater than or equal to {minimum}, was {value}");

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> when <see cref="Guard.MustBeBetweenOrEqualTo{TValue}"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeBetweenOrEqualTo<T>(T value, T minimum, T maximum, string name) =>
        ThrowArgumentOutOfRangeException(
            name,
            $"Parameter \"{name}\" ({typeof(T)}) must be between or equal to {minimum} and {maximum}, was {value}");

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when <see cref="Guard.MustBeSizedAtLeast{T}(ReadOnlySpan{T},int,string)"/> fails.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeExceptionForMustBeSizedAtLeast(int minLength, string parameterName) =>
        ThrowArgumentException(
            FormattableString.Invariant($"Spans must be at least of length {minLength}!"),
            parameterName);

    /// <summary>
    /// Throws a new <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="message">The message to include in the exception.</param>
    /// <param name="name">The argument name.</param>
    /// <exception cref="ArgumentException">Thrown with <paramref name="message"/> and <paramref name="name"/>.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentException(string message, string name) =>
        throw new ArgumentException(message, name);

    /// <summary>
    /// Throws a new <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="message">The message to include in the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown with <paramref name="name"/> and <paramref name="message"/>.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentNullException(string name, string message) =>
        throw new ArgumentNullException(name, message);

    /// <summary>
    /// Throws a new <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="name">The argument name.</param>
    /// <param name="message">The message to include in the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown with <paramref name="name"/> and <paramref name="message"/>.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeException(string name, string message) =>
        throw new ArgumentOutOfRangeException(name, message);
}
