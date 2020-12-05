using System.Runtime.InteropServices;
using System.Security;

namespace System
{
    public static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception ex) => ex is OutOfMemoryException ||
                ex is SecurityException ||
                ex is SEHException;
    }
}
