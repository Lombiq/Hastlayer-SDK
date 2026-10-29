using Hast.Transformer.Vhdl.Tests.Common.Helpers;
using Shouldly.Configuration;

namespace Shouldly;

public static class ShouldMatchConfigurationBuilderExtensions
{
    public static ShouldMatchConfigurationBuilder WithVhdlScrubbers(this ShouldMatchConfigurationBuilder configurationBuilder) =>
        configurationBuilder
            .WithScrubber(VerificationSourceScrubbers.RemoveDateComments)
            .WithScrubber(VerificationSourceScrubbers.RemoveHastIpId);
}
