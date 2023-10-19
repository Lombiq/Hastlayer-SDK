namespace Hast.Communication.Constants;

internal static class CommandTypes
{
    public static readonly byte[] Execution = "x"u8.ToArray();
    public static readonly byte[] WhoIsAvailable = "w"u8.ToArray();
}
