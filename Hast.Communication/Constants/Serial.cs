using System.IO.Ports;

namespace Hast.Communication.Constants.CommunicationConstants;

public static class Serial
{
    public const int DefaultBaudRate = 9_600;
    public const Parity DefaultParity = Parity.None;
    public const StopBits DefaultStopBits = StopBits.One;
    public const int DefaultWriteTimeoutMilliseconds = 10_000;
    public const string ChannelName = nameof(Serial);

    public static class Signals
    {
        public const char Ping = 'p';
        public const char Ready = 'r';
    }

    public enum CommunicationState
    {
        WaitForFirstResponse,
        ReceivingExecutionInformation,
        ReceivingOutputByteCount,
        ReceivingOutput,
        Finished,
    }
}
