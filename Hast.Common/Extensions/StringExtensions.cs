using System.Linq;

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        /// Turns a camelCase or PascalCase token into snake_Case.
        /// </summary>
        /// <param name="str">The input in camelCase or PascalCase.</param>
        /// <returns>
        /// The input converted to snake_Case. It doesn't alter case so you can call either ToUpper or ToLower without
        /// any additional penalties.
        /// </returns>
        public static string ToSnakeCase(this string input) =>
            string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString()));
    }
}
