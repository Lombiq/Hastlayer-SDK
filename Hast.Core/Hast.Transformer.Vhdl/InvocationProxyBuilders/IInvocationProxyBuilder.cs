using Hast.Common.Extensions;
using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.InvocationProxyBuilders;

/// <summary>
/// Builds proxies for external entry points and internal invocations.
/// </summary>
public interface IInvocationProxyBuilder : IDependency
{
    /// <summary>
    /// Returns a collection of internally invoked components.
    /// </summary>
    IEnumerable<IArchitectureComponent> BuildInternalProxy(
        ICollection<IArchitectureComponent> components,
        IVhdlTransformationContext transformationContext);

    /// <summary>
    /// Returns a component representing the hardware entry point.
    /// </summary>
    IArchitectureComponent BuildExternalProxy(
        IEnumerable<IMemberTransformerResult> hardwareEntryPointMemberResults,
        MemberIdTable memberIdTable);
}

public static class InvocationProxyBuilderExtensions
{
    public static (List<IMemberTransformerResult> HardwareEntryPointMemberResults, MemberIdTable MemberIdTable)
        GetHardwareEntryPoints(
        this IInvocationProxyBuilder invocationProxyBuilder,
        IEnumerable<IMemberTransformerResult> transformerResults,
        ICollection<IArchitectureComponent> potentiallyInvokingArchitectureComponents,
        Architecture architecture,
        VhdlTransformationContext vhdlTransformationContext)
    {
        // Proxying external invocations.
        var hardwareEntryPointMemberResults = transformerResults
            .Where(result => result.IsHardwareEntryPointMember)
            .ToList();
        if (!hardwareEntryPointMemberResults.Any())
        {
            throw new InvalidOperationException(
                "There aren't any hardware entry point members, however at least one is needed to execute " +
                "anything on hardware. Did you forget to pass all the assemblies to Hastlayer? Are there " +
                "methods suitable as hardware entry points (see the documentation)?");
        }

        var memberIdTable = BuildMemberIdTable(hardwareEntryPointMemberResults);
        var externalInvocationProxy = invocationProxyBuilder.BuildExternalProxy(hardwareEntryPointMemberResults, memberIdTable);
        potentiallyInvokingArchitectureComponents.Add(externalInvocationProxy);
        architecture.Declarations.Add(externalInvocationProxy.BuildDeclarations());
        architecture.Add(externalInvocationProxy.BuildBody());

        // Proxying internal invocations.
        var internaInvocationProxies = invocationProxyBuilder.BuildInternalProxy(
            potentiallyInvokingArchitectureComponents,
            vhdlTransformationContext);
        foreach (var proxy in internaInvocationProxies)
        {
            architecture.Declarations.Add(proxy.BuildDeclarations());
            architecture.Add(proxy.BuildBody());
        }

        return (hardwareEntryPointMemberResults, memberIdTable);
    }

    private static MemberIdTable BuildMemberIdTable(IEnumerable<IMemberTransformerResult> hardwareEntryPointMemberResults)
    {
        var memberIdTable = new MemberIdTable();
        var memberId = 0;

        foreach (var interfaceMemberResult in hardwareEntryPointMemberResults)
        {
            var methodFullName = interfaceMemberResult.Member.GetFullName();
            memberIdTable.SetMapping(methodFullName, memberId);
            foreach (var methodNameAlternate in methodFullName.GetMemberNameAlternates())
            {
                memberIdTable.SetMapping(methodNameAlternate, memberId);
            }

            memberId++;
        }

        return memberIdTable;
    }
}
