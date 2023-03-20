using Hast.Catapult;
using Hast.Catapult.Drivers;
using Hast.Catapult.Models;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

public class CatapultSamplesVerificationTests : SamplesVerificationTestsBase
{
    protected override bool UseStubMemberSuitabilityChecker => false;
    protected override string DeviceName => CatapultDriver.DeviceName;

    public CatapultSamplesVerificationTests() =>
        _hostConfiguration.Extensions = _hostConfiguration.Extensions.Union(new[] { typeof(CatapultDriver).Assembly });

    [Fact]
    public async Task BasicSamplesMatchApproved() =>
        (await CreateSourceForBasicSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task KpzSamplesMatchesApproved() =>
        (await CreateVhdlForKpzSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task UnumSampleMatchesApproved() =>
        (await CreateVhdlForUnumSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task PositSampleMatchesApproved() =>
        (await CreateVhdlForPositSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task Posit32SampleMatchesApproved() =>
        (await CreateVhdlForPosit32SampleAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task Posit32AdvancedSampleMatchesApproved() =>
        (await CreateSourceForAdvancedPosit32SampleAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task Posit32SampleWithInliningMatchesApproved() =>
        (await CreateVhdlForPosit32SampleWithInliningAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task Posit32FusedSampleMatchesApproved() =>
        (await CreateVhdlForPosit32FusedSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task Fix64SamplesMatchesApproved() =>
        (await CreateVhdlForFix64SamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration();

    [Fact]
    public async Task FSharpSamplesMatchesApproved() =>
        (await CreateVhdlForFSharpSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration();
}
