using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Configuration;
using Hast.Transformer.Vhdl.Configuration;
using Shouldly;
using Xunit;

namespace Hast.NuGet.Vhdl.Tests.VerificationTests;

public class XilinxSamplesVerificationTests
{
    [Fact]
    public async Task ParallelAlgorithmMatchesApproved()
    {
        using var hastlayer = Hastlayer.Create();

        var configuration = new HardwareGenerationConfiguration("Nexys A7");

        configuration.AddHardwareEntryPointType<ParallelAlgorithm>();
        configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration = VhdlGenerationConfiguration.Debug;

        // Using a smaller degree because we don't need excess repetition.
        configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
            new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
            {
                MaxDegreeOfParallelism = 3,
            });

        var hardwareRepresentation = await hastlayer.GenerateHardwareAsync(
            new[]
            {
                typeof(ParallelAlgorithm).Assembly,
            },
            configuration);

        // Basic assertions.
        hardwareRepresentation.DeviceManifest.Name.ShouldBe("Nexys A7");
        hardwareRepresentation.HardwareDescription.Language.ShouldBe("VHDL");

        // Verification of the VHDL source, from file, to make sure it's actually written too.
        var vhdlSource = await File.ReadAllTextAsync(Path.Combine("HardwareFramework", "IPRepo", "Hast_IP.vhd"));
        vhdlSource.ShouldMatchApproved(configuration => configuration.WithVhdlScrubbers());
    }
}
