using System.IO.Ports;
using Hast.Common.Extensibility.Pipeline;
using Hast.Communication.Models;

namespace Hast.Communication.Extensibility.Pipeline
{
    /// <summary>
    /// Extension point for modifying the default configuration used for serial communication.
    /// </summary>
    public interface ISerialPortConfigurator : IPipelineStep
    {
        /// <summary>
        /// Modifies the configuration.
        /// </summary>
        void ConfigureSerialPort(SerialPort serialPort, IHardwareExecutionContext hardwareExecutionContext);
    }
}
