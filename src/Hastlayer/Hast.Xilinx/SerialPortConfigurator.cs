using Hast.Common.Extensibility.Pipeline;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Communication.Models;
using Hast.Xilinx.Drivers;
using System.IO.Ports;

namespace Hast.Xilinx;

public class SerialPortConfigurator : PipelineStepBase, ISerialPortConfigurator
{
    public void ConfigureSerialPort(SerialPort serialPort, IHardwareExecutionContext hardwareExecutionContext)
    {
        if (hardwareExecutionContext.HardwareRepresentation.DeviceManifest.Name is
            not Nexys4DdrDriver.DeviceName and
            not NexysA7Driver.DeviceName)
        {
            return;
        }

        serialPort.BaudRate = 230400;
    }
}
