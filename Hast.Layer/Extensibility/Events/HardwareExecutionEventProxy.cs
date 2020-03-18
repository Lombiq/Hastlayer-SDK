using Hast.Common.Interfaces;
using System;

namespace Hast.Layer.Extensibility.Events
{
    // This needs to be an app-level singleton because it relates to an event member that has the same lifetime as the
    // container inside the Hastlayer object.
    public interface IHardwareExecutionEventHandlerHolder : ISingletonDependency
    {
        void RegisterExecutedOnHardwareEventHandler(Action<ExecutedOnHardwareEventArgs> eventHandler);
        Action<ExecutedOnHardwareEventArgs> GetRegisteredEventHandler();
    }


    public class HardwareExecutionEventHandlerHolder : IHardwareExecutionEventHandlerHolder
    {
        private Action<ExecutedOnHardwareEventArgs> _eventHandler;


        public void RegisterExecutedOnHardwareEventHandler(Action<ExecutedOnHardwareEventArgs> eventHandler) =>
            // No need for locking since this will only be run once during the creation of the Hastlayer instance.
            _eventHandler = eventHandler;

        public Action<ExecutedOnHardwareEventArgs> GetRegisteredEventHandler() => _eventHandler;
    }
}
