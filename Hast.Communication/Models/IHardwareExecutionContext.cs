using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;

namespace Hast.Communication.Models
{
    public interface IHardwareExecutionContext
    {
        IProxyGenerationConfiguration ProxyGenerationConfiguration { get; }
        IHardwareRepresentation HardwareRepresentation { get; }
    }
}
