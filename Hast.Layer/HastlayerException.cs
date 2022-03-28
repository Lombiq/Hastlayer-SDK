using System;
using System.Runtime.Serialization;

namespace Hast.Layer;

/// <summary>
/// Top-level exception thrown from <see cref="IHastlayer"/> implementations.
/// </summary>
[Serializable]
public class HastlayerException : Exception
{
    public HastlayerException(string message)
        : base(message)
    {
    }

    public HastlayerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HastlayerException()
    {
    }

    protected HastlayerException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext) { }
}
