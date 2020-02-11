using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Communication
{
    public class Argument
    {
        public static void ThrowIfNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new NullReferenceException(name);
            }
        }
    }
}
