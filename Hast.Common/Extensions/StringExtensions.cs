using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System;

public static class StringExtensions
{
    /// <summary>
    /// Turns a camelCase or PascalCase token into snake_Case.
    /// </summary>
    /// <param name="input">The input in camelCase or PascalCase.</param>
    /// <returns>
    /// The input converted to snake_Case. It doesn't alter case so you can call either ToUpper or ToLower without any
    /// additional penalties.
    /// </returns>
    /// <remarks>
    /// <para><see href="https://stackoverflow.com/a/18781533"/>.</para>
    /// </remarks>
    public static string ToSnakeCase(this string input) =>
        string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()));

    /// <summary>
    /// Returns <paramref name="text"/> if it's not <see langword="null"/>, empty or whitespace. Otherwise returns
    /// <paramref name="alternative"/>.
    /// </summary>
    public static string OrIfEmpty(this string text, string alternative) =>
        string.IsNullOrWhiteSpace(text) ? alternative : text;

    /// <summary>
    /// Returns <see langword="true"/> if the value is <see langword="true"/> or <c>True</c>.
    /// </summary>
    [SuppressMessage("Minor Code Smell", "S4225:Extension methods should not extend \"object\"", Justification = "No.")]
    public static bool IsTrueString(this object value) => value?.ToString() == bool.TrueString;
}
