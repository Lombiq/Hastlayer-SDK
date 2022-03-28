using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;

namespace Hast.Layer.Extensibility.Events;

public delegate void ExecutedOnHardwareEventHandler(IHastlayer sender, IMemberHardwareExecutionContext e);

public delegate void InvokingEventHandler(IHastlayer sender, IMemberInvocationContext e);
