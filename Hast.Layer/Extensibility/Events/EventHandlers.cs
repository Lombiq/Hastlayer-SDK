using Hast.Communication.Extensibility.Events;
using Hast.Communication.Extensibility.Pipeline;

namespace Hast.Layer.Extensibility.Events
{
    public delegate void ExecutedOnHardwareEventHandler(IHastlayer sender, IMemberHardwareExecutionContext e);
    public delegate void InvokingEventHandler(IHastlayer sender, IMemberInvocationPipelineStepContext e);
}
