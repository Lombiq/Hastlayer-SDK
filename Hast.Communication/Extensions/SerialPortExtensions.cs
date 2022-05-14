namespace System.IO.Ports;

public static class SerialPortExtensions
{
    /// <summary>
    /// Writes a single character to the serial port.
    /// </summary>
    /// <param name="character">A character to write on the serial port.</param>
    public static void Write(this SerialPort serialPort, char character) => serialPort.Write(new[] { character }, 0, 1);
}
