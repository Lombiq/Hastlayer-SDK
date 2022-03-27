using Hast.Common.Validation;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Communication.Services;

public class CommunicationServiceSelector : ICommunicationServiceSelector
{
    private readonly IEnumerable<ICommunicationService> _communicationServices;

    public CommunicationServiceSelector(IEnumerable<ICommunicationService> communicationServices) =>
        _communicationServices = communicationServices;

    public ICommunicationService GetCommunicationService(string channelName = "")
    {
        Argument.ThrowIfNullOrEmpty(channelName, "channelName");
        return _communicationServices.FirstOrDefault(service => service.ChannelName == channelName);
    }
}
