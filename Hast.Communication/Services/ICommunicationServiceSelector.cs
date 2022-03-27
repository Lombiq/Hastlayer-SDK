using Hast.Common.Interfaces;

namespace Hast.Communication.Services;

/// <summary>
/// Service for selecting an <see cref="ICommunicationService"/> instance based on the given channel name.
/// </summary>
public interface ICommunicationServiceSelector : IDependency
{
    /// <summary>
    /// Retrieves the communication service based on the given channel name. If channel name is empty it returns with
    /// the default communication service.
    /// </summary>
    /// <param name="channelName">Name of the channel used to communicate.</param>
    /// <returns>Communication service based on the given channel name.</returns>
    ICommunicationService GetCommunicationService(string channelName = "");
}
