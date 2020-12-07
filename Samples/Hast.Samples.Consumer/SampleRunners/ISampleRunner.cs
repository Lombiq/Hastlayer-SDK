using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Samples.Consumer.SampleRunners
{
    public interface ISampleRunner
    {
        void Configure(HardwareGenerationConfiguration configuration);
        Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration);
    }
}
