using System;
using System.Security.Cryptography;
using System.Text;

namespace Hast.Common.Helpers
{
    public static class Sha2456Helper
    {
        private static Lazy<string> EmptyLazy = new Lazy<string>(() => ComputeHash(string.Empty));


        public static string ComputeHash(string text)
        {
            var hashedIdBytes = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(text));

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < hashedIdBytes.Length; i++)
            {
                stringBuilder.Append(hashedIdBytes[i].ToString("x2"));
            }

            return stringBuilder.ToString();
        }

        public static string Empty() => EmptyLazy.Value;
    }
}
