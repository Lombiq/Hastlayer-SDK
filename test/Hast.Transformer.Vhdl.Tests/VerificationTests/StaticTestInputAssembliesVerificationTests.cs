using Hast.TestInputs.ClassStructure1;
using Hast.TestInputs.ClassStructure2;
using Hast.TestInputs.Static;
using Hast.Transformer.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

public class StaticTestInputAssembliesVerificationTests : VerificationTestFixtureBase
{
    [Fact]
    public Task ClassStructureAssembliesMatchApproved() =>
        Host.RunAsync<ITransformer>(async transformer =>
        {
            var hardwareDescription = await TransformAssembliesToVhdlAsync(
                transformer,
                new[] { typeof(RootClass).Assembly, typeof(StaticReference).Assembly },
                configuration => configuration.TransformerConfiguration().UseSimpleMemory = false);

            hardwareDescription.VhdlSource.ShouldMatchApprovedWithVhdlConfiguration();
        });

    [Fact]
    public Task StaticTestInputAssemblyMatchesApproved() =>
        Host.RunAsync<ITransformer>(async transformer =>
        {
            var hardwareDescription = await TransformAssembliesToVhdlAsync(
                transformer,
                new[] { typeof(ArrayUsingCases).Assembly },
                configuration =>
                {
                    var transformerConfiguration = configuration.TransformerConfiguration();
                    transformerConfiguration.UseSimpleMemory = false;
                    // This shouldn't be necessary: https://github.com/Lombiq/Hastlayer-SDK/issues/112.
                    transformerConfiguration.ArrayLengths.Add(
                        "System.Int32 Hast.TestInputs.Static.ArrayUsingCases+<>c::<PassArrayToTask>b__1_0(System.Object).arrayObject",
                        ArrayUsingCases.TaskArrayLength);

                    configuration
                        .TransformerConfiguration()
                        .AddMemberInvocationInstanceCountConfiguration(
                            new MemberInvocationInstanceCountConfigurationForMethod<ParallelCases>(
                                p => p.WhenAllWhenAnyAwaitedTasks(0), 0)
                            {
                                MaxDegreeOfParallelism = 3,
                            });
                    configuration
                        .TransformerConfiguration()
                        .AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<ParallelCases>(
                            p => p.ObjectUsingTasks(0), 0)
                        {
                            MaxDegreeOfParallelism = 3,
                        });
                });

            hardwareDescription.VhdlSource.ShouldMatchApprovedWithVhdlConfiguration();
        });
}
