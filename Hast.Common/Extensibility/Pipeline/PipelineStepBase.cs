namespace Hast.Common.Extensibility.Pipeline
{
    public abstract class PipelineStepBase : IPipelineStep
    {
        public virtual double Priority { get; protected set; } = 0;
    }
}
