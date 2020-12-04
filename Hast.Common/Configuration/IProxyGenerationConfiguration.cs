using System.Collections.Generic;

namespace Hast.Layer
{
    public interface IProxyGenerationConfiguration
    {
        /// <summary>
        /// Gets a dictionary that can contain settings for non-default configuration options.
        /// </summary>
        IDictionary<string, object> CustomConfiguration { get; }

        /// <summary>
        /// Gets the communication channel used for communicating with the FPGA (eg. Ethernet).
        /// </summary>
        string CommunicationChannelName { get; }

        /// <summary>
        /// Gets or whether the results coming from the hardware implementation should be checked against a software 
        /// execution. If set to <c>true</c> then both a hardware and software invocation will happen and the result of 
        /// the two compared. If there is a mismatch then an exception will be thrown.
        /// </summary>
        bool VerifyHardwareResults { get; }
    }

    public static class ProxyGenerationConfigurationExtensions
    {
        /// <summary>
        /// Gets the custom configuration if it exists or creates and adds it if it doesn't.
        /// </summary>
        /// <typeparam name="T">Type of the configuration object.</typeparam>
        /// <param name="key">Key where the custom configuration object is stored in the 
        /// <see cref="IProxyGenerationConfiguration"/> instance.</param>
        /// <returns>The existing or newly created configuration object.</returns>
        public static T GetOrAddCustomConfiguration<T>(this IProxyGenerationConfiguration proxyGenerationConfiguration, string key)
            where T : new() =>
            proxyGenerationConfiguration.CustomConfiguration.GetOrAddCustomConfiguration<T>(key);
    }
}
