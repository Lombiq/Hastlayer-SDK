using System;
using System.Runtime.Serialization;

namespace Hast.Communication.Exceptions;

/// <summary>
/// This exception is thrown when something is wrong with the FPGA board connected through Ethernet connection.
/// </summary>
[Serializable]
public class EthernetCommunicationException : Exception
{
    public EthernetCommunicationException(string message)
        : base(message) { }

    public EthernetCommunicationException(string message, Exception innerException)
        : base(message, innerException) { }

    public EthernetCommunicationException() { }

    protected EthernetCommunicationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
