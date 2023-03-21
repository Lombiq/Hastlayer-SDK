using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

public static class ArchitectureComponentDataObjectExtensions
{
    public static IEnumerable<TypedDataObject> GetAllDataObjects(this IArchitectureComponent component) =>
        component.GlobalVariables
            .Cast<TypedDataObject>()
            .Union(component.LocalVariables)
            .Union(component.InternallyDrivenSignals)
            .Union(component.ExternallyDrivenSignals);

    public static IEnumerable<Signal> GetAllSignals(this IArchitectureComponent component) =>
        component.InternallyDrivenSignals.Union(component.ExternallyDrivenSignals);

    public static IEnumerable<Variable> GetAllVariables(this IArchitectureComponent component) =>
        component.GlobalVariables.Union(component.LocalVariables);

    public static IEnumerable<ParameterSignal> GetInParameterSignals(this IArchitectureComponent component) =>
        FilterParameterSignals(component.ExternallyDrivenSignals);

    public static IEnumerable<ParameterSignal> GetOutParameterSignals(this IArchitectureComponent component) =>
        FilterParameterSignals(component.InternallyDrivenSignals);

    public static Variable CreateVariableWithNextUnusedIndexedName(
        this IArchitectureComponent component,
        string name,
        DataType dataType)
    {
        var variable = new Variable
        {
            Name = component.GetNextUnusedIndexedObjectName(name),
            DataType = dataType,
        };

        component.LocalVariables.Add(variable);

        return variable;
    }

    public static Signal CreateSignalWithNextUnusedIndexedName(
        this IArchitectureComponent component,
        string name,
        DataType dataType)
    {
        var signal = new Signal
        {
            Name = component.GetNextUnusedIndexedObjectName(name),
            DataType = dataType,
        };

        component.InternallyDrivenSignals.Add(signal);

        return signal;
    }

    public static DataObjectReference CreateParameterSignalReference(
        this IArchitectureComponent component,
        string parameterName,
        ParameterFlowDirection direction) =>
        ArchitectureComponentNameHelper
            .CreateParameterSignalName(component.Name, parameterName, direction)
            .ToVhdlSignalReference();

    public static DataObjectReference CreateReturnSignalReference(this IArchitectureComponent component) =>
        ArchitectureComponentNameHelper
            .CreateReturnSignalName(component.Name)
            .ToVhdlSignalReference();

    public static DataObjectReference CreateReturnSignalReferenceForTargetComponent(
        this IArchitectureComponent component,
        string targetMemberName,
        int index) =>
        component
            .CreatePrefixedSegmentedObjectName(targetMemberName, NameSuffixes.Return, index.ToTechnicalString())
            .ToVhdlSignalReference();

    private static IEnumerable<ParameterSignal> FilterParameterSignals(IEnumerable<Signal> signals) =>
        signals.Where(signal => signal is ParameterSignal).Cast<ParameterSignal>();
}
