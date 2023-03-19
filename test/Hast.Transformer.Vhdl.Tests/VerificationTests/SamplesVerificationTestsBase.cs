using Hast.Algorithms;
using Hast.Algorithms.Random;
using Hast.Common.Models;
using Hast.Layer;
using Hast.Samples.FSharpSampleAssembly;
using Hast.Samples.Kpz.Algorithms;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Configuration;
using Lombiq.Arithmetics;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

/// <summary>
/// Base for tests that cover the samples. Needs to be done in such a way, test methods can't be in the base class
/// (NUnit limitation), nor can Shouldly matching happen here (since it needs to be configured from the actual caller).
/// </summary>
public abstract class SamplesVerificationTestsBase : VerificationTestFixtureBase
{
    protected override bool UseStubMemberSuitabilityChecker => false;

    protected Task<VhdlHardwareDescription> CreateSourceForBasicSamplesAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(RandomMwc64X).Assembly },
            configuration =>
            {
                // Only testing well-tested samples.

                var transformerConfiguration = configuration.TransformerConfiguration();

                configuration.AddHardwareEntryPointType<GenomeMatcher>();

                // Verify that [Replaceable] works.
                var replacementName = nameof(MonteCarloPiEstimator) + "." +
                                      nameof(MonteCarloPiEstimator.MaxDegreeOfParallelism);
                configuration.GetOrAddReplacements()[replacementName] = 123;

                // Not configuring MaxDegreeOfParallelism for ImageContrastModifier to also test the logic that can
                // figure it out.
                configuration.AddHardwareEntryPointType<ImageContrastModifier>();

                configuration.AddHardwareEntryPointType<Loopback>();

                configuration.AddHardwareEntryPointType<MemoryTest>();

                configuration.AddHardwareEntryPointType<MonteCarloPiEstimator>();
                transformerConfiguration.AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<MonteCarloPiEstimator>(m => m.EstimatePi(null), 0)
                    {
                        MaxDegreeOfParallelism = 3, // Using a smaller degree because we don't need excess repetition.
                    });
                configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomXorshiftLfsr16>(p => p.NextUInt16());

                configuration.AddHardwareEntryPointType<ObjectOrientedShowcase>();

                configuration.AddHardwareEntryPointType<ParallelAlgorithm>();
                transformerConfiguration.AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<ParallelAlgorithm>(p => p.Run(null), 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });

                configuration.AddHardwareEntryPointType<PrimeCalculator>();
                transformerConfiguration.AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<PrimeCalculator>(p => p.ParallelizedArePrimeNumbers(default), 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });

                configuration.AddHardwareEntryPointType<RecursiveAlgorithms>();
                transformerConfiguration.AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<RecursiveAlgorithms>("Recursively")
                    {
                        MaxRecursionDepth = 3,
                    });

                configuration.AddHardwareEntryPointType<HastlayerAcceleratedImageSharp>();
                transformerConfiguration.AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<HastlayerAcceleratedImageSharp>(p => p.CreateMatrix(null), 0)
                    {
                        MaxRecursionDepth = 3,
                    });

                configuration.AddHardwareEntryPointType<SimdCalculator>();
            }));

    protected async Task<VhdlHardwareDescription[]> CreateVhdlForKpzSamplesAsync() =>
        new[]
        {
            await Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
                provider.GetService<ITransformer>(),
                new[] { typeof(KpzKernelsInterface).Assembly, typeof(RandomMwc64X).Assembly },
                configuration =>
                {
                    configuration.AddHardwareEntryPointType<KpzKernelsInterface>();

                    configuration.AddHardwareEntryPointType<KpzKernelsParallelizedInterface>();
                    configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<KpzKernelsParallelizedInterface>(
                            p => p.ScheduleIterations(null),
                            lambdaExpressionIndex: 0)
                        {
                            MaxDegreeOfParallelism = 3,
                        });
                })),
            await Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
                provider.GetService<ITransformer>(),
                new[] { typeof(KpzKernelsInterface).Assembly, typeof(RandomMwc64X).Assembly },
                configuration =>
                {
                    configuration.AddHardwareEntryPointType<KpzKernelsInterface>();

                    configuration.AddHardwareEntryPointType<KpzKernelsParallelizedInterface>();
                    configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                        new MemberInvocationInstanceCountConfigurationForMethod<KpzKernelsParallelizedInterface>(
                            p => p.ScheduleIterations(null),
                            lambdaExpressionIndex: 0)
                        {
                            MaxDegreeOfParallelism = 3,
                        });
                    configuration.TransformerConfiguration().AddAdditionalInlinableMethod<RandomMwc64X>(r => r.NextUInt32());
                })),
        };

    protected Task<VhdlHardwareDescription> CreateVhdlForUnumSampleAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Unum).Assembly, typeof(ImmutableArray).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<UnumCalculator>();

                configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                    UnumCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                    UnumCalculatorExtensions.ManuallySizedArrays);
            }));

    protected Task<VhdlHardwareDescription> CreateVhdlForPositSampleAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly, typeof(ImmutableArray).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<PositCalculator>();
                configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                    PositCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                    PositCalculatorExtensions.ManuallySizedArrays);
            }));

    protected Task<VhdlHardwareDescription> CreateVhdlForPosit32SampleAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<Posit32Calculator>();
                configuration.TransformerConfiguration().EnableMethodInlining = false;

                configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<Posit32Calculator>(
                        p => p.ParallelizedCalculateIntegerSumUpToNumbers(null),
                        lambdaExpressionIndex: 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });
            }));

    protected Task<VhdlHardwareDescription> CreateVhdlForPosit32SampleWithInliningAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<Posit32Calculator>();

                configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<Posit32Calculator>(
                        p => p.ParallelizedCalculateIntegerSumUpToNumbers(null),
                        lambdaExpressionIndex: 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });
            }));

    protected Task<VhdlHardwareDescription> CreateSourceForAdvancedPosit32SampleAsync() =>
        Host.RunGetAsync(async provider => await TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<Posit32AdvancedCalculator>();
                configuration.TransformerConfiguration().EnableMethodInlining = false;
            }));

    protected async Task<VhdlHardwareDescription[]> CreateVhdlForPosit32FusedSampleAsync() =>
        new[]
        {
            await Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
                provider.GetService<ITransformer>(),
                new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly },
                configuration =>
                {
                    configuration.AddHardwareEntryPointType<Posit32FusedCalculator>();
                    configuration.TransformerConfiguration().EnableMethodInlining = false;
                    configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                        Posit32.QuireSize >> 6,
                        Posit32FusedCalculatorExtensions.ManuallySizedArrays);
                })),
            await Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
                provider.GetService<ITransformer>(),
                new[] { typeof(PrimeCalculator).Assembly, typeof(Posit).Assembly },
                configuration =>
                {
                    configuration.AddHardwareEntryPointType<Posit32FusedCalculator>();
                    configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                        Posit32.QuireSize >> 6,
                        Posit32FusedCalculatorExtensions.ManuallySizedArrays);
                })),
        };

    protected Task<VhdlHardwareDescription> CreateVhdlForFix64SamplesAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(PrimeCalculator).Assembly, typeof(Fix64).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<Fix64Calculator>();

                configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<Fix64Calculator>(
                        f => f.ParallelizedCalculateIntegerSumUpToNumbers(default),
                        lambdaExpressionIndex: 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });
            }));

    protected Task<VhdlHardwareDescription> CreateVhdlForFSharpSamplesAsync() =>
        Host.RunGetAsync(provider => TransformAssembliesToVhdlAsync(
            provider.GetService<ITransformer>(),
            new[] { typeof(FSharpParallelAlgorithmContainer).Assembly },
            configuration =>
            {
                configuration.AddHardwareEntryPointType<FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm>();

                configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                    new MemberInvocationInstanceCountConfigurationForMethod<FSharpParallelAlgorithmContainer.FSharpParallelAlgorithm>(
                        f => f.Run(null),
                        lambdaExpressionIndex: 0)
                    {
                        MaxDegreeOfParallelism = 3,
                    });
            }));
}
