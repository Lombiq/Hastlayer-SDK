using System;

namespace Hast.Layer
{
    /// <summary>
    /// Top-level exception thrown from <see cref="IHastlayer"/> implementations.
    /// </summary>
    public class HastlayerException : Exception
    {
        public HastlayerException(string message) : base(message)
        {
        }

        public HastlayerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
