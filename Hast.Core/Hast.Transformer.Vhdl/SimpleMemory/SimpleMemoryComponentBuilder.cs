using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.SimpleMemory;

public class SimpleMemoryComponentBuilder : ISimpleMemoryComponentBuilder
{
    private readonly ISimpleMemoryOperationProxyBuilder _simpleMemoryOperationProxyBuilder;

    public SimpleMemoryComponentBuilder(ISimpleMemoryOperationProxyBuilder simpleMemoryOperationProxyBuilder) =>
        _simpleMemoryOperationProxyBuilder = simpleMemoryOperationProxyBuilder;

    public void AddSimpleMemoryComponentsToArchitecture(
        IEnumerable<IArchitectureComponent> invokingComponents,
        Architecture architecture)
    {
        // Proxying SimpleMemory operations
        var simpleMemoryProxyComponent = _simpleMemoryOperationProxyBuilder.BuildProxy(invokingComponents);
        architecture.Declarations.Add(simpleMemoryProxyComponent.BuildDeclarations());
        architecture.Add(simpleMemoryProxyComponent.BuildBody());

        // Adding common ports
        var ports = architecture.Entity.Ports;
        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.DataIn.ToExtendedVhdlId(),
            Mode = PortMode.In,
            DataType = SimpleMemoryTypes.DataSignalsDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.DataOut.ToExtendedVhdlId(),
            Mode = PortMode.Out,
            DataType = SimpleMemoryTypes.DataSignalsDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.CellIndex.ToExtendedVhdlId(),
            Mode = PortMode.Out,
            DataType = SimpleMemoryTypes.CellIndexSignalDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.ReadEnable.ToExtendedVhdlId(),
            Mode = PortMode.Out,
            DataType = SimpleMemoryTypes.EnableSignalsDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.WriteEnable.ToExtendedVhdlId(),
            Mode = PortMode.Out,
            DataType = SimpleMemoryTypes.EnableSignalsDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.ReadsDone.ToExtendedVhdlId(),
            Mode = PortMode.In,
            DataType = SimpleMemoryTypes.DoneSignalsDataType,
        });

        ports.Add(new Port
        {
            Name = SimpleMemoryPortNames.WritesDone.ToExtendedVhdlId(),
            Mode = PortMode.In,
            DataType = SimpleMemoryTypes.DoneSignalsDataType,
        });
    }
}
