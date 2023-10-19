using Hast.Common.Models;
using Hast.TestInputs.ClassStructure1;
using Hast.TestInputs.ClassStructure2;
using Hast.Transformer.Configuration;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests;

public class BasicHardwareStructureTests : VhdlTransformingTestFixtureBase
{
    [Fact]
    public Task BasicHardwareDescriptionPropertiesAreCorrect()
    {
        VhdlManifest manifest = null;

        _hostConfiguration.OnServiceRegistration += (_, services) =>
            services.AddSingleton(new EventHandler<TransformedVhdlManifest>((_, e) => manifest = e.Manifest));

        return Host.RunAsync<ITransformer>(async transformer =>
        {
            var hardwareDescription = await TransformClassStrutureExamplesToVhdlAsync(transformer);

            hardwareDescription.Language.ShouldBe("VHDL");
            hardwareDescription.HardwareEntryPointNamesToMemberIdMappings.Count.ShouldBe(14);
            hardwareDescription.VhdlSource.ShouldNotBeNullOrEmpty();
            manifest.ShouldNotBeNull(); // Since caching is off.
        });
    }

    [Fact]
    public Task BasicVhdlStructureIsCorrect()
    {
        VhdlManifest manifest = null;

        _hostConfiguration.OnServiceRegistration += (_, services) =>
            services.AddSingleton(new EventHandler<TransformedVhdlManifest>((_, e) => manifest = e.Manifest));

        return Host.RunAsync<ITransformer>(async transformer =>
         {
             await TransformClassStrutureExamplesToVhdlAsync(transformer);
             var topModule = (Module)manifest.Modules[^1];

             var architecture = topModule.Architecture;
             architecture.Name.ShouldNotBeNullOrEmpty();
             architecture.Declarations.ShouldRecursivelyContain(element => element is Signal);
             architecture.Body.ShouldRecursivelyContain<Process>(
                 p => p.Name.Contains("ExternalInvocationProxy", StringComparison.InvariantCulture));

             var entity = topModule.Entity;
             entity.Name.ShouldNotBeNullOrEmpty();
             entity.Ports.Count.ShouldBe(5);
             entity.ShouldBe(topModule.Architecture.Entity, "The top module's entity is not referenced by the architecture.");

             topModule.Libraries.Any().ShouldBeTrue();
         });
    }

    private Task<VhdlHardwareDescription> TransformClassStrutureExamplesToVhdlAsync(ITransformer transformer) =>
        TransformAssembliesToVhdlAsync(
            transformer,
            new[] { typeof(RootClass).Assembly, typeof(StaticReference).Assembly },
            configuration => configuration.TransformerConfiguration().UseSimpleMemory = false);
}
