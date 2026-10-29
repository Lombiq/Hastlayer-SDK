using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Common.Extensibility.Pipeline;

public static class PipelineStepEnumerationExtensions
{
    public static IEnumerable<T> PrioritizePipelineSteps<T>(this IEnumerable<T> pipelineSteps)
        where T : IPipelineStep =>
        pipelineSteps.OrderByDescending(step => step.Priority);

    public static void InvokePipelineSteps<T>(this IEnumerable<T> pipelineSteps, Action<T> invoke)
        where T : IPipelineStep
    {
        foreach (var step in pipelineSteps.PrioritizePipelineSteps())
        {
            invoke(step);
        }
    }
}
