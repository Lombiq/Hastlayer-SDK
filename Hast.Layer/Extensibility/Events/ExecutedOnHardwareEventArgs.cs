using System;

namespace Hast.Layer.Extensibility.Events
{
    public class ExecutedOnHardwareEventArgs : EventArgs
    {
        private IHardwareRepresentation _hardwareRepresentation;
        public IHardwareRepresentation HardwareRepresentation
        {
            get { return _hardwareRepresentation; }
        }

        private string _memberFullName;
        public string MemberFullName
        {
            get { return _memberFullName; }
        }

        private IHardwareExecutionInformation _hardwareExecutionInformation;
        public IHardwareExecutionInformation HardwareExecutionInformation
        {
            get { return _hardwareExecutionInformation; }
        }


        public ExecutedOnHardwareEventArgs(
            IHardwareRepresentation hardwareRepresentation,
            string memberFullName,
            IHardwareExecutionInformation hardwareExecutionInformation)
        {
            _hardwareRepresentation = hardwareRepresentation;
            _memberFullName = memberFullName;
            _hardwareExecutionInformation = hardwareExecutionInformation;
        }
    }
}
