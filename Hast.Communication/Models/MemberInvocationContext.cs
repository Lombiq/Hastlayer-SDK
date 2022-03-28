using Castle.DynamicProxy;
using Hast.Communication.Extensibility.Events;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Layer;

namespace Hast.Communication.Models;

public class MemberInvocationContext : IMemberInvocationPipelineStepContext, IMemberHardwareExecutionContext
{
    public bool HardwareExecutionIsCancelled { get; set; }
    public IInvocation Invocation { get; set; }
    public string MemberFullName { get; set; }
    public IHardwareRepresentation HardwareRepresentation { get; set; }
    public IHardwareExecutionInformation HardwareExecutionInformation { get; set; }
    public ISoftwareExecutionInformation SoftwareExecutionInformation { get; set; }
}
