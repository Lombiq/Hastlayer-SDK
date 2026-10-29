using Hast.Layer;
using Hast.TestInputs.Invalid;
using Hast.Transformer.Configuration;
using Shouldly;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Transformer.Vhdl.Tests;

public class TransformationCheckTests : VhdlTransformingTestFixtureBase
{
    [Fact]
    public Task InvalidExternalVariableAssignmentIsPrevented() =>
        Host.RunAsync<ITransformer>(async transformer =>
            await Should.ThrowAsync(
                async () => await TransformInvalidTestInputsAsync<InvalidParallelCases>(transformer, c => c.InvalidExternalVariableAssignment(0)),
                typeof(NotSupportedException)));

    [Fact]
    public Task InvalidArrayUsageIsPrevented() =>
        Host.RunAsync<ITransformer>(async transformer =>
        {
            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.InvalidArrayAssignment()),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.ArraySizeIsNotStatic(0)),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.ArrayCopyToIsNotStaticCopy(0)),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.UnsupportedImmutableArrayCreateRangeUsage()),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.MultiDimensionalArray()),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidArrayUsingCases>(transformer, c => c.NullAssignment()),
                typeof(NotSupportedException));
        });

    [Fact]
    public Task InvalidHardwareEntryPointsArePrevented() =>
        Host.RunAsync<ITransformer>(async transformer =>
            await Should.ThrowAsync(
                async () => await TransformInvalidTestInputsAsync<InvalidHardwareEntryPoint>(transformer, c => c.EntryPointMethod()),
                typeof(NotSupportedException)));

    [Fact]
    public Task InvalidLanguageConstructsArePrevented() =>
        Host.RunAsync<ITransformer>(async transformer =>
        {
            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidLanguageConstructCases>(transformer, c => c.CustomValueTypeReferenceEquals()),
                typeof(InvalidOperationException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidLanguageConstructCases>(transformer, c => c.InvalidModelUsage()),
                typeof(NotSupportedException));
        });

    [Fact]
    public Task InvalidInvalidObjectUsingCasesArePrevented() =>
        Host.RunAsync<ITransformer>(async transformer =>
        {
            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidObjectUsingCases>(transformer, c => c.ReferenceAssignment(0)),
                typeof(NotSupportedException));

            await Should.ThrowAsync(
                () => TransformInvalidTestInputsAsync<InvalidObjectUsingCases>(transformer, c => c.SelfReferencingType()),
                typeof(NotSupportedException));
        });

    private async Task TransformInvalidTestInputsAsync<T>(
        ITransformer transformer,
        Expression<Action<T>> expression,
        bool useSimpleMemory = false)
    {
        await TransformAssembliesToVhdlAsync(
            transformer,
            [typeof(InvalidParallelCases).Assembly],
            configuration =>
            {
                configuration.TransformerConfiguration().UseSimpleMemory = useSimpleMemory;
                configuration.AddHardwareEntryPointMethod(expression);
            });

        return;
    }
}
