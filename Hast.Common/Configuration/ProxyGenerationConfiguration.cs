using System.Collections.Generic;

namespace Hast.Layer
{
    public class ProxyGenerationConfiguration : IProxyGenerationConfiguration
    {
        /// <summary>
        /// Gets or sets a dictionary that can contain settings for non-default configuration options (like the name of
        /// the communication channel).
        /// </summary>
        public IDictionary<string, object> CustomConfiguration { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the communication channel used for communicating with the hardware device (eg. Ethernet).
        /// </summary>
        public string CommunicationChannelName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the results coming from the hardware implementation should be checked against a
        /// software execution. If set to <see langword="true"/> then both a hardware and software invocation will happen and the
        /// result of the two compared. If there is a mismatch then an exception will be thrown.
        /// </summary>
        public bool VerifyHardwareResults { get; set; }

        private static IProxyGenerationConfiguration _default;
        public static IProxyGenerationConfiguration Default
        {
            get
            {
                _default ??= new ProxyGenerationConfiguration();

                return _default;
            }
        }
    }
}
