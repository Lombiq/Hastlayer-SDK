using Hast.Common.Interfaces;
using Hast.Transformer.SimpleMemory;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SimpleMemory;

/// <summary>
/// Service used for building <see cref="SimpleMemory"/> component.
/// </summary>
public interface ISimpleMemoryComponentBuilder : IDependency
{
    /// <summary>
    /// Proxies the <see cref="SimpleMemory"/> operations and adds the common
    /// ports to the architecture component.
    /// </summary>
    void AddSimpleMemoryComponentsToArchitecture(
        IEnumerable<IArchitectureComponent> invokingComponents,
        Architecture architecture);
}
