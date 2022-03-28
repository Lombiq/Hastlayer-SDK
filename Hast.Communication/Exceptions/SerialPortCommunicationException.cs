using System;
using System.Runtime.Serialization;

namespace Hast.Communication.Exceptions;

/// <summary>
/// This exception is thrown when something is wrong with the FPGA board connected through serial connection.
/// </summary>
[Serializable]
public class SerialPortCommunicationException : Exception
{
    public SerialPortCommunicationException(string message)
        : base(message) { }

    public SerialPortCommunicationException(string message, Exception inner)
        : base(message, inner) { }

    public SerialPortCommunicationException() { }

    protected SerialPortCommunicationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext) { }
}
