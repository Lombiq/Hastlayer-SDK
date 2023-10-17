using Hast.VhdlBuilder.Extensions;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

public enum ParameterFlowDirection
{
    In,
    Out,
}

public static class ArchitectureComponentNameHelper
{
    public static string CreateParameterSignalName(string componentName, string parameterName, ParameterFlowDirection direction) =>
        CreatePrefixedSegmentedObjectName(componentName, parameterName, NameSuffixes.Parameter, direction.ToString());

    public static string CreateReturnSignalName(string componentName) =>
        CreatePrefixedObjectName(componentName, NameSuffixes.Return);

    public static string CreateStartedSignalName(string componentName) =>
        CreatePrefixedObjectName(componentName, NameSuffixes.Started);

    public static string CreateFinishedSignalName(string componentName) =>
        CreatePrefixedObjectName(componentName, NameSuffixes.Finished);

    public static string CreateIndexedComponentName(string componentName, int index) =>
        StringHelper.CreateInvariant($"{componentName}.{index}");

    public static string CreatePrefixedSegmentedObjectName(string componentName, params string[] segments) =>
        CreatePrefixedObjectName(componentName, string.Join(".", segments));

    public static string CreatePrefixedObjectName(string componentName, string name) =>
        CreatePrefixedExtendedVhdlId(componentName, "." + name);

    public static string CreatePrefixedExtendedVhdlId(string componentName, string id) =>
        (componentName + id).ToExtendedVhdlId();
}
