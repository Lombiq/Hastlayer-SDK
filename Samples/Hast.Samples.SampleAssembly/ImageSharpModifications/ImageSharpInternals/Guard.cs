// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Modified file, original is found at:
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Common/Helpers/Guard.cs
// https://github.com/SixLabors/SharedInfrastructure/blob/master/src/SharedInfrastructure/Guard.cs

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hast.Samples.SampleAssembly.ImageSharpModifications
{
    /// <summary>
    /// Provides methods to protect against invalid parameters.
    /// </summary>
    [DebuggerStepThrough]
    internal static partial class Guard
    {
        /// <summary>
        /// Ensures that the value is not null.
        /// </summary>
        /// <param name="value">The target object, which cannot be null.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<TValue>(TValue value, string parameterName)
            where TValue : class
        {
            if (!(value is null))
            {
                return;
            }

            ThrowHelper.ThrowArgumentNullExceptionForNotNull(parameterName);
        }

        /// <summary>
        /// Ensures that the target value is not null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The target string, which should be checked against being null or empty.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is empty or contains only blanks.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrWhiteSpace(string value, string parameterName)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            ThrowHelper.ThrowArgumentExceptionForNotNullOrWhitespace(value, parameterName);
        }

        /// <summary>
        /// Ensures that the specified value is less than a maximum value.
        /// </summary>
        /// <param name="value">The target value, which should be validated.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is greater than the maximum value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeLessThan<TValue>(TValue value, TValue max, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) < 0)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeLessThan(value, max, parameterName);
        }

        /// <summary>
        /// Verifies that the specified value is less than or equal to a maximum value
        /// and throws an exception if it is not.
        /// </summary>
        /// <param name="value">The target value, which should be validated.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is greater than the maximum value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeLessThanOrEqualTo<TValue>(TValue value, TValue max, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) <= 0)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeLessThanOrEqualTo(value, max, parameterName);
        }

        /// <summary>
        /// Verifies that the specified value is greater than a minimum value
        /// and throws an exception if it is not.
        /// </summary>
        /// <param name="value">The target value, which should be validated.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is less than the minimum value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeGreaterThan<TValue>(TValue value, TValue min, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) > 0)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeGreaterThan(value, min, parameterName);
        }

        /// <summary>
        /// Verifies that the specified value is greater than or equal to a minimum value
        /// and throws an exception if it is not.
        /// </summary>
        /// <param name="value">The target value, which should be validated.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is less than the minimum value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeGreaterThanOrEqualTo<TValue>(TValue value, TValue min, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) >= 0)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeGreaterThanOrEqualTo(value, min, parameterName);
        }

        /// <summary>
        /// Verifies that the specified value is greater than or equal to a minimum value and less than
        /// or equal to a maximum value and throws an exception if it is not.
        /// </summary>
        /// <param name="value">The target value, which should be validated.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is less than the minimum value of greater than the maximum value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeBetweenOrEqualTo<TValue>(TValue value, TValue min, TValue max, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeBetweenOrEqualTo(value, min, max, parameterName);
        }

        /// <summary>
        /// Verifies, that the method parameter with specified target value is true
        /// and throws an exception if it is found to be so.
        /// </summary>
        /// <param name="target">The target value, which cannot be false.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <param name="message">The error message, if any to add to the exception.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="target"/> is false.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool target, string parameterName, string message)
        {
            if (target)
            {
                return;
            }

            ThrowHelper.ThrowArgumentException(message, parameterName);
        }

        /// <summary>
        /// Verifies, that the method parameter with specified target value is false
        /// and throws an exception if it is found to be so.
        /// </summary>
        /// <param name="target">The target value, which cannot be true.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <param name="message">The error message, if any to add to the exception.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="target"/> is true.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsFalse(bool target, string parameterName, string message)
        {
            if (!target)
            {
                return;
            }

            ThrowHelper.ThrowArgumentException(message, parameterName);
        }

        /// <summary>
        /// Verifies, that the `source` span has the length of 'minLength', or longer.
        /// </summary>
        /// <typeparam name="T">The element type of the spans.</typeparam>
        /// <param name="source">The source span.</param>
        /// <param name="minLength">The minimum length.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="source"/> has less than <paramref name="minLength"/> items.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeSizedAtLeast<T>(ReadOnlySpan<T> source, int minLength, string parameterName)
        {
            if (source.Length >= minLength)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeSizedAtLeast(minLength, parameterName);
        }

        /// <summary>
        /// Verifies, that the `source` span has the length of 'minLength', or longer.
        /// </summary>
        /// <typeparam name="T">The element type of the spans.</typeparam>
        /// <param name="source">The target span.</param>
        /// <param name="minLength">The minimum length.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="source"/> has less than <paramref name="minLength"/> items.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeSizedAtLeast<T>(Span<T> source, int minLength, string parameterName)
        {
            if (source.Length >= minLength)
            {
                return;
            }

            ThrowHelper.ThrowArgumentOutOfRangeExceptionForMustBeSizedAtLeast(minLength, parameterName);
        }

        /// <summary>
        /// Verifies that the 'destination' span is not shorter than 'source'.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TDest">The destination element type.</typeparam>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span.</param>
        /// <param name="destinationParamName">The name of the argument for 'destination'.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestinationShouldNotBeTooShort<TSource, TDest>(
            ReadOnlySpan<TSource> source,
            Span<TDest> destination,
            string destinationParamName)
        {
            if (destination.Length >= source.Length)
            {
                return;
            }

            ThrowHelper.ThrowArgumentException("Destination span is too short!", destinationParamName);
        }

        /// <summary>
        /// Verifies that the 'destination' span is not shorter than 'source'.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <typeparam name="TDest">The destination element type.</typeparam>
        /// <param name="source">The source span.</param>
        /// <param name="destination">The destination span.</param>
        /// <param name="destinationParamName">The name of the argument for 'destination'.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestinationShouldNotBeTooShort<TSource, TDest>(
            Span<TSource> source,
            Span<TDest> destination,
            string destinationParamName)
        {
            if (destination.Length >= source.Length)
            {
                return;
            }

            ThrowHelper.ThrowArgumentException("Destination span is too short!", destinationParamName);
        }

        /// <summary>
        /// Ensures that the value is a value type.
        /// </summary>
        /// <param name="value">The target object, which cannot be null.</param>
        /// <param name="parameterName">The name of the parameter that is to be checked.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a value type.</exception>
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void MustBeValueType<TValue>(TValue value, string parameterName)
        {
            if (value.GetType()
#if NETSTANDARD1_3
                .GetTypeInfo()
#endif
                .IsValueType)
            {
                return;
            }

            ThrowHelper.ThrowArgumentException("Type must be a struct.", parameterName);
        }
    }
}
