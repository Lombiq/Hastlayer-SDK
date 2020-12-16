using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hast.Common.Helpers
{
    public static class Sha256Helper
    {
        private static readonly Lazy<string> _emptyLazy = new Lazy<string>(() => ComputeHash(string.Empty));

        public static string ComputeHash(string text)
        {
            using var sha256 = new SHA256Managed();
            var hashedIdBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < hashedIdBytes.Length; i++)
            {
                stringBuilder.Append(hashedIdBytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return stringBuilder.ToString();
        }

        public static string Empty() => _emptyLazy.Value;
    }
}
