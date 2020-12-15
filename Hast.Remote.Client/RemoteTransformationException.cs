using System;

namespace Hast.Remote.Client
{
    public class RemoteTransformationException : Exception
    {
        public RemoteTransformationException(string message) : base(message)
        {
        }

        public RemoteTransformationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
