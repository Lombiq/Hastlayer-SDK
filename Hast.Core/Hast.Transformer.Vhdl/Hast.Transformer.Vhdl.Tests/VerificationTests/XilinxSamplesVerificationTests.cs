using Hast.Xilinx.Abstractions.ManifestProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

public class XilinxSamplesVerificationTests : SamplesVerificationTestsBase
{
    private static readonly string[] _devicesToTest =
    {
        Nexys4DdrManifestProvider.DeviceName,
        AlveoU50ManifestProvider.DeviceName,
    };

    public static IEnumerable<object[]> AllDevices => _devicesToTest.Select(name => new object[] { name });

    [Theory, MemberData(nameof(AllDevices))]
    public async Task BasicSamplesMatchApproved(string deviceName) =>
        (await CreateSourceForBasicSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task KpzSamplesMatchesApproved(string deviceName) =>
        (await CreateVhdlForKpzSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task UnumSampleMatchesApproved(string deviceName) =>
        (await CreateVhdlForUnumSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task PositSampleMatchesApproved(string deviceName) =>
        (await CreateVhdlForPositSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task Posit32SampleMatchesApproved(string deviceName) =>
        (await CreateVhdlForPosit32SampleAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task Posit32AdvancedSampleMatchesApproved(string deviceName) =>
        (await CreateSourceForAdvancedPosit32SampleAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task Posit32SampleWithInliningMatchesApproved(string deviceName) =>
        (await CreateVhdlForPosit32SampleWithInliningAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task Posit32FusedSampleMatchesApproved(string deviceName) =>
        (await CreateVhdlForPosit32FusedSampleAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task Fix64SamplesMatchesApproved(string deviceName) =>
        (await CreateVhdlForFix64SamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);

    [Theory, MemberData(nameof(AllDevices))]
    public async Task FSharpSamplesMatchesApproved(string deviceName) =>
        (await CreateVhdlForFSharpSamplesAsync()).ShouldMatchApprovedWithVhdlConfiguration(deviceName);
}
