using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Common;
using Hast.Layer;
using Orchard;

namespace Hast.Synthesis.Abstractions
{
    public interface IHardwareImplementationComposer : IDependency
    {
        /// <summary>
        /// Composes the hardware implementation for the given hardware representation.
        /// </summary>
        /// <param name="hardwareDescription">Represents the hardware that was generated from .NET assemblies.</param>
        /// <returns>The handle to the synthesized hardware implementation.</returns>
        Task<IHardwareImplementation> Compose(IHardwareDescription hardwareDescription);
    }
}
