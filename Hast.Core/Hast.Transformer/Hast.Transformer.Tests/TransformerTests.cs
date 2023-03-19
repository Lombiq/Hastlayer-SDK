using Hast.Common.Services;
using Hast.Layer;
using Hast.Synthesis.Services;
using Hast.TestInputs.ClassStructure1;
using Hast.TestInputs.ClassStructure1.ComplexTypes;
using Hast.TestInputs.ClassStructure2;
using Hast.Transformer.Configuration;
using Hast.Transformer.Models;
using Hast.Transformer.Services;
using Hast.Xilinx;
using Hast.Xilinx.ManifestProviders;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests;

public class TransformerTests
{
    private readonly AutoMocker _mocker;
    private ITransformationContext _producedContext;

    public TransformerTests()
    {
        _mocker = new AutoMocker();

        // The order here is important because some of the earlier services are dependencies of later ones.
        _mocker.Use<IJsonConverter>(_mocker.CreateInstance<DefaultJsonConverter>());
        _mocker.Use<IMemberSuitabilityChecker>(_mocker.CreateInstance<MemberSuitabilityChecker>());
        _mocker.Use<ITypeDeclarationLookupTableFactory>(_mocker.CreateInstance<TypeDeclarationLookupTableFactory>());
        _mocker.Use<ISyntaxTreeCleaner>(_mocker.CreateInstance<SyntaxTreeCleaner>());
        _mocker.Use<ITransformationContextCacheService>(_mocker.CreateInstance<TransformationContextCacheService>());

        // Moq has a problem with resolving IEnumerable<Tservice> in the constructor even when Tservice is already
        // registered, so these have to be added manually. See: https://github.com/moq/Moq.AutoMocker/issues/76
        _mocker.Use<IEnumerable<EventHandler<ITransformationContext>>>(Array.Empty<EventHandler<ITransformationContext>>());
        _mocker.Use<IDeviceDriverSelector>(new DeviceDriverSelector(new[] { _mocker.CreateInstance<Nexys4DdrDriver>() }));

        _mocker.Use<IEnumerable<IConverter>>(
            new[]
            {
                _mocker.CreateInstance<MemberIdentifiersFixer>(),
                MockConverter<UnneededReferenceVariablesRemover>(),
            });

        _mocker
            .GetMock<ITransformingEngine>()
            .Setup(engine => engine.TransformAsync(It.IsAny<ITransformationContext>()))
            .Returns<ITransformationContext>(context =>
                {
                    // Sending out the context through a field is not a nice solutions but there doesn't seem to be a
                    // better one.
                    _producedContext = context;
                    return Task.FromResult<IHardwareDescription>(null);
                })
            .Verifiable();

        _mocker.Use<ITransformerPostProcessor>(_mocker.CreateInstance<DefaultTransformerPostProcessor>());
    }

    [Fact]
    public async Task TransformEngineCallReceivesProperBasicContext()
    {
        var configuration = CreateConfig();
        var transformer = GetTransformer();

        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, configuration);

        _mocker
            .GetMock<ITransformingEngine>()
            .Verify(engine => engine.TransformAsync(It.Is<ITransformationContext>(context => context != null)));

        _producedContext.Id.ShouldNotBeNullOrEmpty();
        _producedContext.SyntaxTree.ShouldNotBeNull();
        configuration.ShouldBe(
            _producedContext.HardwareGenerationConfiguration,
            "The input hardware generation configuration was not properly passed on to the transformation context.");
        _producedContext.TypeDeclarationLookupTable.ShouldNotBeNull();
    }

    [Fact]
    public async Task DifferentConfigurationsResultInDifferentIds()
    {
        var config = CreateConfig();
        var transformer = GetTransformer();

        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        var firstId = _producedContext.Id;
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly, typeof(StaticReference).Assembly }, config);
        firstId.ShouldNotBe(
            _producedContext.Id,
            "The transformation context ID isn't different despite the set of assemblies transformed being different.");

        config.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
            new MemberInvocationInstanceCountConfiguration("Hast.TestInputs.ClassStructure1.RootClass.VirtualMethod")
            {
                MaxDegreeOfParallelism = 5,
            });
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId = _producedContext.Id;
        config.TransformerConfiguration().MemberInvocationInstanceCountConfigurations.Single().MaxDegreeOfParallelism = 15;
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId.ShouldNotBe(
            _producedContext.Id,
            "The transformation context ID isn't different despite the max degree of parallelism being different.");

        config.HardwareEntryPointMemberFullNames.Add("aaa");
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId = _producedContext.Id;
        config.HardwareEntryPointMemberFullNames.Add("bbb");
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId.ShouldNotBe(
            _producedContext.Id,
            "The transformation context ID isn't different despite the set of included members being different.");

        config.HardwareEntryPointMemberNamePrefixes.Add("aaa");
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId = _producedContext.Id;
        config.HardwareEntryPointMemberNamePrefixes.Add("bbb");
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly }, config);
        firstId.ShouldNotBe(
            _producedContext.Id,
            "The transformation context ID isn't different despite the set of included members prefixed being different.");
    }

    [Fact]
    public async Task UnusedDeclarationsArentInTheSyntaxTree()
    {
        var transformer = GetTransformer();
        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly, typeof(StaticReference).Assembly }, CreateConfig());
        var typeLookup = BuildTypeLookup();

        typeLookup.Count.ShouldBe(7, "Not the number of types remained in the syntax tree than there are used.");
        typeLookup.ShouldNotContainKey(nameof(UnusedDeclarations), "Classes with unreferenced members weren't removed from the syntax tree.");
        typeLookup[nameof(ComplexTypeHierarchy)].Members.ShouldNotContain(
            member => member.Name == "UnusedMethod" || member.Name == "NonVirtualNonInterfaceMehod",
            "Unreferenced members of classes weren't removed from the syntax tree.");
    }

    [Fact]
    public async Task IncludedMembersAndTheirReferencesAreOnlyInTheSyntaxTree()
    {
        var configuration = CreateConfig();
        configuration.HardwareEntryPointMemberFullNames
            .Add($"System.Void {typeof(RootClass).FullName}::{nameof(RootClass.VirtualMethod)}(System.Int32)");
        configuration.HardwareEntryPointMemberFullNames
            .Add($"System.Void {typeof(ComplexTypeHierarchy).FullName}::" +
                 $"{typeof(IInterface1).FullName}.{nameof(IInterface1.Interface1Method1)}()");
        var transformer = GetTransformer();

        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly, typeof(StaticReference).Assembly }, configuration);
        var typeLookup = BuildTypeLookup();

        typeLookup.Count.ShouldBe(3, "Not the number of types remained in the syntax tree than there are used.");
        typeLookup[nameof(RootClass)].Members.Count.ShouldBe(1);
        typeLookup[nameof(RootClass)].Members.Single().Name.ShouldBe("VirtualMethod");
        typeLookup[nameof(ComplexTypeHierarchy)].Members.Count.ShouldBe(3);
        typeLookup[nameof(ComplexTypeHierarchy)].Members.Select(member => member.Name)
            .SequenceEqual(new[] { "Interface1Method1", "PrivateMethod", "StaticMethod" })
            .ShouldBeTrue();
        typeLookup[nameof(IInterface1)].Members.Count.ShouldBe(1);
        typeLookup[nameof(IInterface1)].Members.Select(member => member.Name)
            .SequenceEqual(new[] { "Interface1Method1" })
            .ShouldBeTrue();
    }

    [Fact]
    public async Task IncludedMembersPrefixedAndTheirReferencesAreOnlyInTheSyntaxTree()
    {
        var configuration = CreateConfig();
        configuration.HardwareEntryPointMemberNamePrefixes.Add($"{typeof(RootClass).FullName}.{nameof(RootClass.VirtualMethod)}");
        configuration.HardwareEntryPointMemberNamePrefixes.Add(typeof(ComplexTypeHierarchy).Namespace);
        var transformer = GetTransformer();

        await transformer.TransformAsync(new[] { typeof(ComplexTypeHierarchy).Assembly, typeof(StaticReference).Assembly }, configuration);
        var typeLookup = BuildTypeLookup();

        typeLookup.Count.ShouldBe(5, "Not the number of types remained in the syntax tree than there are used.");
        typeLookup[nameof(RootClass)].Members.Count.ShouldBe(1);
        typeLookup[nameof(RootClass)].Members.Single().Name.ShouldBe("VirtualMethod");
        typeLookup[nameof(ComplexTypeHierarchy)].Members.Count.ShouldBe(7);
        typeLookup[nameof(ComplexTypeHierarchy)].Members
            .Select(member => member.Name)
            .SequenceEqual(new[]
            {
                "Interface1Method1",
                "Interface1Method2",
                "Interface2Method1",
                "BaseInterfaceMethod1",
                "BaseInterfaceMethod2",
                "PrivateMethod",
                "StaticMethod",
            })
            .ShouldBeTrue();
    }

    private ITransformer GetTransformer() => _mocker.CreateInstance<DefaultTransformer>();

    private Dictionary<string, TypeDeclaration> BuildTypeLookup() =>
        _producedContext.SyntaxTree.GetAllTypeDeclarations().ToDictionary(type => type.Name);

    private static IConverter MockConverter<T>() =>
        new CustomConverter
        {
            Name = typeof(T).Name,
            ConverterAction = (_, _, _) => { },
        };

    private static HardwareGenerationConfiguration CreateConfig()
    {
        var configuration = new HardwareGenerationConfiguration(Nexys4DdrManifestProvider.DeviceName, hardwareFrameworkPath: null);
        configuration.TransformerConfiguration().UseSimpleMemory = false;
        return configuration;
    }
}
