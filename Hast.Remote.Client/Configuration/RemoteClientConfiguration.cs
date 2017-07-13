using System;

namespace Hast.Remote.Configuration
{
    public class RemoteClientConfiguration
    {
        public Uri EndpointBaseUri { get; set; } = new Uri("https://hastlayer.com/api/Hastlayer.Frontend");
        public string AppId { get; set; }
        public string AppSecret { get; set; }
    }
}
