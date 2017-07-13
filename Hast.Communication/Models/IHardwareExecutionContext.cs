using Hast.Layer;

namespace Hast.Communication.Models
{
    public interface IHardwareExecutionContext
    {
        IProxyGenerationConfiguration ProxyGenerationConfiguration { get; }
        IHardwareRepresentation HardwareRepresentation { get; }
    }
}
