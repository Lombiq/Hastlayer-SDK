using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Remote.Configuration
{
    public class RemoteClientConfiguration
    {
        public Uri EndpointBaseUri { get; set; } = new Uri("https://hastlayer.com/api/Hastlayer.Frontend");
        public string AppId { get; set; }
        public string AppSecret { get; set; }
    }
}
