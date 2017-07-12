using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Common.Helpers
{
    public static class Sha2456Helper
    {
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
    }
}
