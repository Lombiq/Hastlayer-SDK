namespace Hast.Communication.Constants.CommunicationConstants
{
    public static class Ethernet
    {
        public const string ChannelName = nameof(Ethernet);

        public static class Ports
        {
            public const int WhoIsAvailableRequest = 34050;
            public const int WhoIsAvailableResponse = 33000;
        }

        public static class Signals
        {
            public const char Busy = 'b';
            public const char Ready = 'r';
        }
    }
}
