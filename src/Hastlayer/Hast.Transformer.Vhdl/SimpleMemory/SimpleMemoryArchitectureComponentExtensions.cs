using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System.Linq;

namespace Hast.Transformer.Vhdl.SimpleMemory;

/// <summary>
/// Handles intermediary signals for using SimpleMemory ports. Such signals are only needed for Out or InOut ports, In
/// ports can be simply read from multiple places; so intermediary signals are only needed for the CellIndex, DataOut,
/// ReadEnable and WriteEnable ports.
/// </summary>
public static class SimpleMemoryArchitectureComponentExtensions
{
    public static bool AreSimpleMemorySignalsAdded(this IArchitectureComponent component)
    {
        // If there is a signal for CellIndex then all the others should be added too.
        var signalName = component.CreateSimpleMemorySignalName(SimpleMemoryPortNames.CellIndex);
        return component.InternallyDrivenSignals.Any(signal => signal.Name == signalName);
    }

    public static void AddSimpleMemorySignalsIfNew(this IArchitectureComponent component)
    {
        if (component.AreSimpleMemorySignalsAdded()) return;

        component.InternallyDrivenSignals.Add(new Signal
        {
            DataType = SimpleMemoryTypes.CellIndexInternalSignalDataType,
            Name = component.CreateSimpleMemorySignalName(SimpleMemoryPortNames.CellIndex),
        });
        component.InternallyDrivenSignals.Add(new Signal
        {
            DataType = SimpleMemoryTypes.DataSignalsDataType,
            Name = component.CreateSimpleMemorySignalName(SimpleMemoryPortNames.DataOut),
        });
        component.InternallyDrivenSignals.Add(new Signal
        {
            DataType = SimpleMemoryTypes.EnableSignalsDataType,
            Name = component.CreateSimpleMemorySignalName(SimpleMemoryPortNames.ReadEnable),
            InitialValue = Value.False,
        });
        component.InternallyDrivenSignals.Add(new Signal
        {
            DataType = SimpleMemoryTypes.EnableSignalsDataType,
            Name = component.CreateSimpleMemorySignalName(SimpleMemoryPortNames.WriteEnable),
            InitialValue = Value.False,
        });
    }

    public static DataObjectReference CreateSimpleMemoryCellIndexSignalReference(this IArchitectureComponent component) =>
        component.CreateSimpleMemorySignalReference(SimpleMemoryPortNames.CellIndex);

    public static DataObjectReference CreateSimpleMemoryDataOutSignalReference(this IArchitectureComponent component) =>
        component.CreateSimpleMemorySignalReference(SimpleMemoryPortNames.DataOut);

    public static DataObjectReference CreateSimpleMemoryReadEnableSignalReference(this IArchitectureComponent component) =>
        component.CreateSimpleMemorySignalReference(SimpleMemoryPortNames.ReadEnable);

    public static DataObjectReference CreateSimpleMemoryWriteEnableSignalReference(this IArchitectureComponent component) =>
        component.CreateSimpleMemorySignalReference(SimpleMemoryPortNames.WriteEnable);

    public static DataObjectReference CreateSimpleMemorySignalReference(
        this IArchitectureComponent component,
        string simpleMemoryPortName) =>
        component.CreateSimpleMemorySignalName(simpleMemoryPortName).ToVhdlSignalReference();

    public static string CreateSimpleMemorySignalName(
        this IArchitectureComponent component,
        string simpleMemoryPortName) =>
        component.CreatePrefixedSegmentedObjectName("SimpleMemory", simpleMemoryPortName).ToExtendedVhdlId();
}
