using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Models;
using Hast.Transformer.Vhdl.Constants;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.InvocationProxyBuilders;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.SimpleMemory;
using Hast.Transformer.Vhdl.SubTransformers;
using Hast.Transformer.Vhdl.Verifiers;
using Hast.VhdlBuilder;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Module = Hast.VhdlBuilder.Representation.Declaration.Module;

namespace Hast.Transformer.Vhdl.Services;

public class TransformedVhdlManifestBuilder : ITransformedVhdlManifestBuilder
{
    private readonly IEnumerable<IVerifyer> _verifiers;
    private readonly IClock _clock;
    private readonly IInvocationProxyBuilder _invocationProxyBuilder;
    private readonly Lazy<ISimpleMemoryComponentBuilder> _simpleMemoryComponentBuilderLazy;
    private readonly IRemainderOperatorExpressionsExpander _remainderOperatorExpressionsExpander;
    private readonly IMemberTransformer _memberTransformer;
    private readonly ITypesCreator _typesCreator;
    private readonly IEnumerable<IXdcFileBuilder> _xdcFileBuilders;

    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "All of these are necessary for VHDL manifest building.")]
    public TransformedVhdlManifestBuilder(
        IEnumerable<IVerifyer> verifiers,
        IClock clock,
        IInvocationProxyBuilder invocationProxyBuilder,
        Lazy<ISimpleMemoryComponentBuilder> simpleMemoryComponentBuilderLazy,
        IRemainderOperatorExpressionsExpander remainderOperatorExpressionsExpander,
        IMemberTransformer memberTransformer,
        ITypesCreator typesCreator,
        IEnumerable<IXdcFileBuilder> xdcFileBuilders)
    {
        _verifiers = verifiers;
        _clock = clock;
        _invocationProxyBuilder = invocationProxyBuilder;
        _simpleMemoryComponentBuilderLazy = simpleMemoryComponentBuilderLazy;
        _remainderOperatorExpressionsExpander = remainderOperatorExpressionsExpander;
        _memberTransformer = memberTransformer;
        _typesCreator = typesCreator;
        _xdcFileBuilders = xdcFileBuilders;
    }

    public async Task<TransformedVhdlManifest> BuildManifestAsync(ITransformationContext transformationContext)
    {
        var syntaxTree = transformationContext.SyntaxTree;

        // Running verifications.
        foreach (var verifier in _verifiers) verifier.Verify(syntaxTree, transformationContext);

        // Running AST changes.
        _remainderOperatorExpressionsExpander.ExpandRemainderOperatorExpressions(syntaxTree);

        var vhdlTransformationContext = new VhdlTransformationContext(transformationContext);
        var useSimpleMemory = transformationContext.GetTransformerConfiguration().UseSimpleMemory;

        var hastIpArchitecture = new Architecture { Name = "Imp" };
        var hastIpModule = new Module { Architecture = hastIpArchitecture };

        // Adding libraries
        hastIpModule.Libraries.Add(new Library(
            name: "ieee",
            uses: new List<string> { "std_logic_1164.all", "numeric_std.all" }));

        // Creating the Hast_IP entity. Its name can't be an extended identifier.
        var hastIpEntity = hastIpModule.Entity = new Entity { Name = Entity.ToSafeEntityName("Hast_IP") };
        hastIpArchitecture.Entity = hastIpEntity;

        var generationDateTimeUtcText = _clock.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

        hastIpEntity.Declarations.Add(new UnOmittableLineComment("Hast_IP ID: " + transformationContext.Id));
        hastIpEntity.Declarations.Add(new UnOmittableLineComment("Date and time: " + generationDateTimeUtcText));
        hastIpEntity.Declarations.Add(new UnOmittableLineComment("Generated by Hastlayer - hastlayer.com"));

        var portsComment = useSimpleMemory ? LongGeneratedSimpleMemoryCodeComments.Ports : LongGeneratedCodeComments.Ports;
        hastIpEntity.Declarations.Add(new LogicalBlock(new BlockComment(portsComment)));

        hastIpArchitecture.Declarations.Add(new BlockComment(LongGeneratedCodeComments.Overview));

        var dependentTypesTables = new List<DependentTypesTable>();

        _typesCreator.CreateTypes(
            syntaxTree,
            vhdlTransformationContext,
            dependentTypesTables,
            hastIpArchitecture);

        // Doing transformations.
        var transformerResults = await Task.WhenAll(
            _memberTransformer.TransformMembers(transformationContext.SyntaxTree, vhdlTransformationContext));
        var warnings = new List<ITransformationWarning>();
        var potentiallyInvokingArchitectureComponents = transformerResults
            .SelectMany(result => result
                .ArchitectureComponentResults
                .Select(componentResult =>
                {
                    warnings.AddRange(componentResult.Warnings);
                    return componentResult.ArchitectureComponent;
                }))
            .ToList();
        var architectureComponentResults = transformerResults
            .SelectMany(transformerResult => transformerResult.ArchitectureComponentResults)
            .ToList();
        var architectureComponentResultsWithDependentTypes = architectureComponentResults
            .Where(architectureComponentResult => architectureComponentResult.ArchitectureComponent.DependentTypesTable.Types.Any())
            .Select(architectureComponentResult => architectureComponentResult.ArchitectureComponent.DependentTypesTable);
        dependentTypesTables.AddRange(architectureComponentResultsWithDependentTypes);
        var deviceManifest = transformationContext.DeviceDriver.DeviceManifest;
        var xdcFileBuilders = _xdcFileBuilders
            .Where(builder => builder.IsTargetType(deviceManifest))
            .ToList();
        xdcFileBuilders.Sort();
        var xdcFile = xdcFileBuilders.LastOrDefault() is { } deepestXdcProvider
            ? await deepestXdcProvider.BuildManifestAsync(architectureComponentResults, hastIpArchitecture)
            : null;

        // Processing inter-dependent types. In VHDL if a type depends another type (e.g. an array stores elements of a
        // record type) than the type depending on the other one should come after the other one in the code file.
        var allDependentTypes = dependentTypesTables
            .SelectMany(table => table.Types)
            .GroupBy(type => type.Name) // A dependency relation can be present multiple times, so need to group first.
            .ToDictionary(group => group.Key, group => group.First());
        var sortedDependentTypes = TopologicalSortHelper.Sort(
            allDependentTypes.Values,
            sortedType => dependentTypesTables
                .SelectMany(table => table.GetDependencies(sortedType))
                .Where(type => type != null && allDependentTypes.ContainsKey(type))
                .Select(type => allDependentTypes[type]));
        if (sortedDependentTypes.Any())
        {
            var dependentTypesDeclarationsBlock = new LogicalBlock(new LineComment("Custom inter-dependent type declarations start"));
            dependentTypesDeclarationsBlock.Body.AddRange(sortedDependentTypes);
            dependentTypesDeclarationsBlock.Add(new LineComment("Custom inter-dependent type declarations end"));
            hastIpArchitecture.Declarations.Add(dependentTypesDeclarationsBlock);
        }

        // Adding architecture component declarations. These should come after custom inter-dependent type declarations.
        foreach (var architectureComponentResult in architectureComponentResults)
        {
            hastIpArchitecture.Declarations.Add(architectureComponentResult.Declarations);
            hastIpArchitecture.Add(architectureComponentResult.Body);
        }

        // Proxying invocations.
        var (hardwareEntryPointMemberResults, memberIdTable) = _invocationProxyBuilder.GetHardwareEntryPoints(
            transformerResults,
            potentiallyInvokingArchitectureComponents,
            hastIpArchitecture,
            vhdlTransformationContext);

        // Proxying SimpleMemory operations.
        if (useSimpleMemory)
        {
            _simpleMemoryComponentBuilderLazy.Value.AddSimpleMemoryComponentsToArchitecture(
                potentiallyInvokingArchitectureComponents,
                hastIpArchitecture);
        }

        // Adding common ports.
        var ports = hastIpEntity.Ports;
        ports.Add(new Port
        {
            Name = CommonPortNames.MemberId,
            Mode = PortMode.In,
            DataType = KnownDataTypes.UnrangedInt,
        });
        ports.Add(new Port
        {
            Name = CommonPortNames.Reset,
            Mode = PortMode.In,
            DataType = KnownDataTypes.StdLogic,
        });
        ports.Add(new Port
        {
            Name = CommonPortNames.Started,
            Mode = PortMode.In,
            DataType = KnownDataTypes.Boolean,
        });
        ports.Add(new Port
        {
            Name = CommonPortNames.Finished,
            Mode = PortMode.Out,
            DataType = KnownDataTypes.Boolean,
        });

        ProcessUtility.AddClockToProcesses(hastIpModule, CommonPortNames.Clock);

        var manifest = new VhdlManifest();

        manifest.Modules.Add(new UnOmittableBlockComment(
            new[] { "Generated by Hastlayer (hastlayer.com) at " + generationDateTimeUtcText + " for the following hardware entry points: " }
            .Union(hardwareEntryPointMemberResults.Select(result => "* " + result.Member.GetFullName()))
            .ToArray()));
        manifest.Modules.Add(new Raw(Environment.NewLine));

        manifest.Modules.Add(new BlockComment(LongGeneratedCodeComments.Libraries));

        // If the TypeConversion functions change those changes need to be applied to the Timing Tester app too.
        ReadAndAddEmbedLibrary("TypeConversion", manifest, hastIpModule);

        if (useSimpleMemory)
        {
            ReadAndAddEmbedLibrary("SimpleMemory", manifest, hastIpModule);
        }

        manifest.Modules.Add(new LineComment("Hast_IP, logic generated from the input .NET assemblies starts here."));
        manifest.Modules.Add(hastIpModule);

        return new TransformedVhdlManifest
        {
            Manifest = manifest,
            MemberIdTable = memberIdTable,
            Warnings = warnings,
            XdcFile = xdcFile,
        };
    }

    private static void ReadAndAddEmbedLibrary(
        string libraryName,
        VhdlManifest manifest,
        Module hastIpModule)
    {
        var resourceName = "Hast.Transformer.Vhdl.VhdlLibraries." + libraryName + ".vhd";
#pragma warning disable S3902 // "Assembly.GetExecutingAssembly" should not be called. Except we are in a library.
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
#pragma warning restore S3902 // "Assembly.GetExecutingAssembly" should not be called. Except we are in a library.
        using (var reader = new StreamReader(stream!))
        {
            manifest.Modules.Add(new LogicalBlock(new Raw(reader.ReadToEnd())));
        }

        hastIpModule.Libraries.Add(new Library(
            name: "work",
            uses: new List<string> { libraryName + ".all" }));
    }
}