using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Remote.Client;

[SuppressMessage(
    "Major Code Smell",
    "S3925:\"ISerializable\" should be implemented correctly",
    Justification = "This exception shouldn't be serialized.")]
public class RemoteTransformationException : Exception
{
    public RemoteTransformationException(string message)
        : base(message)
    {
    }

    public RemoteTransformationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RemoteTransformationException()
    {
    }
}
