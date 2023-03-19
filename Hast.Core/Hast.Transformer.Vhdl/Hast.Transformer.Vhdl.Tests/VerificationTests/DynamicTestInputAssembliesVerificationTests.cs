using Hast.TestInputs.Dynamic;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

public class DynamicTestInputAssembliesVerificationTests : VerificationTestFixtureBase
{
    protected override bool UseStubMemberSuitabilityChecker => false;

    [Fact]
    public Task DynamicTestInputAssemblyMatchesApproved() => Host.RunAsync<ITransformer>(
        async transformer =>
        {
            var hardwareDescription = await TransformAssembliesToVhdlAsync(
                transformer,
                new[] { typeof(BinaryAndUnaryOperatorExpressionCases).Assembly },
                _ => { });

            hardwareDescription.VhdlSource.ShouldMatchApprovedWithVhdlConfiguration();
        });
}
