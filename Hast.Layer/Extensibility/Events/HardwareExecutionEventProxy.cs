using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;
using System;

namespace Hast.Layer.Extensibility.Events
{
    // This needs to be an app-level singleton so it's not recreated with shell restarts (otherwise enabling/disabling
    // features for example would cause the registered event handler to be lost. It can't have the same implementation
    // as IMemberInvocationEventHandler because than the letter's lifetime scope-level lifetime would take precedence.
    public interface IHardwareExecutionEventHandlerHolder
    {
        void RegisterExecutedOnHardwareEventHandler(Action<ExecutedOnHardwareEventArgs> eventHandler);
        Action<ExecutedOnHardwareEventArgs> GetRegisteredEventHandler();
    }


    public class HardwareExecutionEventHandlerHolder : IHardwareExecutionEventHandlerHolder
    {
        private Action<ExecutedOnHardwareEventArgs> _eventHandler;


        public void RegisterExecutedOnHardwareEventHandler(Action<ExecutedOnHardwareEventArgs> eventHandler) =>
            // No need for locking since this will only be run once in a shell.
            _eventHandler = eventHandler;

        public Action<ExecutedOnHardwareEventArgs> GetRegisteredEventHandler() => _eventHandler;
    }


    public class HardwareExecutionEventProxy : IMemberInvocationEventHandler
    {
        private readonly IHardwareExecutionEventHandlerHolder _eventHandlerHolder;


        public HardwareExecutionEventProxy(IHardwareExecutionEventHandlerHolder eventHandlerHolder)
        {
            _eventHandlerHolder = eventHandlerHolder;
        }


        public void MemberInvoking(IMemberInvocationContext invocationContext) { }

        public void MemberExecutedOnHardware(IMemberHardwareExecutionContext hardwareExecutionContext)
        {
            _eventHandlerHolder.GetRegisteredEventHandler()(
                new ExecutedOnHardwareEventArgs(
                    hardwareExecutionContext.HardwareRepresentation,
                    hardwareExecutionContext.MemberFullName,
                    hardwareExecutionContext.HardwareExecutionInformation,
                    hardwareExecutionContext.SoftwareExecutionInformation));
        }
    }
}
