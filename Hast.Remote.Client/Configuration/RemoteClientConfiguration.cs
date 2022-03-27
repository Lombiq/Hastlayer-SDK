using System;

namespace Hast.Remote.Configuration;

public class RemoteClientConfiguration
{
    /// <summary>
    /// Gets or sets the URI of the Hastlayer Remote Services endpoint. Defaults to the URI of the public service.
    /// </summary>
    public Uri EndpointBaseUri { get; set; } = new Uri("https://hastlayer.com/api/Hastlayer.Frontend");

    /// <summary>
    /// Gets or sets the name of the application registered in Hastlayer Remote Services to authenticate against the
    /// remote transformation service.
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// Gets or sets the secret of the application registered in Hastlayer Remote Services to authenticate against the
    /// remote transformation service.
    /// </summary>
    public string AppSecret { get; set; }
}
