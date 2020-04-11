using System;

namespace Hast.Layer.Extensibility.Events
{
    public class ExecutedOnHardwareEventArgs : EventArgs
    {
        public IHardwareRepresentation HardwareRepresentation { get; }
        public string MemberFullName { get; }
        public IHardwareExecutionInformation HardwareExecutionInformation { get; }
        public ISoftwareExecutionInformation SoftwareExecutionInformation { get; }


        public ExecutedOnHardwareEventArgs(
            IHardwareRepresentation hardwareRepresentation,
            string memberFullName,
            IHardwareExecutionInformation hardwareExecutionInformation,
            ISoftwareExecutionInformation softwareExecutionInformation)
        {
            HardwareRepresentation = hardwareRepresentation;
            MemberFullName = memberFullName;
            HardwareExecutionInformation = hardwareExecutionInformation;
            SoftwareExecutionInformation = softwareExecutionInformation;
        }
    }
}
