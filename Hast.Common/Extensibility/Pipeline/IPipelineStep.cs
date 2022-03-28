using Hast.Common.Interfaces;

namespace Hast.Common.Extensibility.Pipeline;

/// <summary>
/// Represents a step in a pipeline of operations that are executed in priority order.
/// </summary>
/// <remarks>
/// <para>Pipelines differ from events in that they are executed in a deterministic order.</para>
/// </remarks>
public interface IPipelineStep : IDependency
{
    /// <summary>
    /// Gets the priority value of the pipeline step. The priority affects the order in which pipeline steps are
    /// executed after each other: A bigger number means higher priority and pipeline steps with a higher priority are
    /// executed before the ones with a lower priority.
    /// </summary>
    double Priority { get; }
}
