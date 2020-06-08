using Hast.Common.Interfaces;
using Hast.Layer;

namespace Hast.Common.Services
{
    public interface IHardwareGenerationConfigurationHolder : IDependency
    {
        IHardwareGenerationConfiguration Configuration { get; set; }
    }
}
