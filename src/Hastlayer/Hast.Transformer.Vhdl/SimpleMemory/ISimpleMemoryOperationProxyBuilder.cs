using Hast.Common.Interfaces;
using Hast.Transformer.SimpleMemory;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SimpleMemory;

/// <summary>
/// Service for proxying <see cref="SimpleMemory"/> operations.
/// </summary>
public interface ISimpleMemoryOperationProxyBuilder : IDependency
{
    /// <summary>
    /// Creates a <c>SimpleMemoryOperationProxy</c> component.
    /// </summary>
    IArchitectureComponent BuildProxy(IEnumerable<IArchitectureComponent> components);
}
