using System;
using System.Runtime.Serialization;

namespace Hast.Remote.Client
{
    [Serializable]
    public class RemoteTransformationException : Exception
    {
        public RemoteTransformationException(string message)
            : base(message) { }

        public RemoteTransformationException(string message, Exception innerException)
            : base(message, innerException) { }

        public RemoteTransformationException() { }

        protected RemoteTransformationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}
