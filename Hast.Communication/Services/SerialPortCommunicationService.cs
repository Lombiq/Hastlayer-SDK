using Hast.Common.Extensibility.Pipeline;
using Hast.Communication.Constants;
using Hast.Communication.Constants.CommunicationConstants;
using Hast.Communication.Exceptions;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Hast.Communication.Services;

public class SerialPortCommunicationService : CommunicationServiceBase
{
    private const int MemoryPrefixCellCount = 3;
    private const int FirstCellPadding = sizeof(int) - 1;

    private readonly IDevicePoolPopulator _devicePoolPopulator;
    private readonly IDevicePoolManager _devicePoolManager;
    private readonly IEnumerable<ISerialPortConfigurator> _serialPortConfigurators;
    private readonly ILogger<SerialPortCommunicationService> _logger;

    public override string ChannelName => Serial.ChannelName;

    public SerialPortCommunicationService(
        IDevicePoolPopulator devicePoolPopulator,
        IDevicePoolManager devicePoolManager,
        IEnumerable<ISerialPortConfigurator> serialPortConfigurators,
        ILogger<SerialPortCommunicationService> logger)
        : base(logger)
    {
        _devicePoolPopulator = devicePoolPopulator;
        _devicePoolManager = devicePoolManager;
        _serialPortConfigurators = serialPortConfigurators;
        _logger = logger;
    }

    public override async Task<IHardwareExecutionInformation> ExecuteAsync(
        SimpleMemory simpleMemory,
        int memberId,
        IHardwareExecutionContext executionContext)
    {
        _devicePoolPopulator.PopulateDevicePoolIfNew(async () =>
            {
                var portNames = await GetFpgaPortNamesAsync(executionContext);
                return portNames.Select(portName => new Device { Identifier = portName });
            });

        using var device = await _devicePoolManager.ReserveDeviceAsync();
        var context = BeginExecution();

        // Initializing some serial port connection settings (may be different with some FPGA boards). For detailed info
        // on how the SerialPort class works see:
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/e36193cd-a708-42b3-86b7-adff82b19e5e Also we might
        // consider this: http://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport

        using var serialPort = CreateSerialPort(executionContext);
        serialPort.PortName = device.Identifier;

        try
        {
            // We try to open the serial port.
            serialPort.Open();
        }
        catch (IOException ex)
        {
            throw new SerialPortCommunicationException(
                "Communication with the FPGA board through the serial port failed. Probably the FPGA board is not connected.",
                ex);
        }

        if (serialPort.IsOpen)
        {
            Logger.LogInformation("The port {PortName} is ours.", serialPort.PortName);
        }
        else
        {
            throw new SerialPortCommunicationException(
                "Communication with the FPGA board through the serial port failed. The " +
                serialPort.PortName + " exists but it's used by another process.");
        }

        // Here we put together the data stream.

        // Prepare memory.
        var accessor = new SimpleMemoryAccessor(simpleMemory);
        // The first parameter is actually just a byte but we only fetch whole cells so 3/4 of the cell is padding.
        var memory = accessor.Get(MemoryPrefixCellCount)[FirstCellPadding..];
        var memoryLength = simpleMemory.ByteCount;

        // Execute Order 66.
        // Set command type
        var commandType = (byte)CommandTypes.Execution;
        MemoryMarshal.Write(memory.Span, ref commandType);
        // Copying the input length, represented as bytes, to the output buffer.
        MemoryMarshal.Write(memory.Span.Slice(1, sizeof(int)), ref memoryLength);
        // Copying the member ID, represented as bytes, to the output buffer.
        MemoryMarshal.Write(memory.Span.Slice(1 + sizeof(int), sizeof(int)), ref memberId);

        // Sending the data. Just using serialPort.Write() once with all the data would stop sending data after 16372
        // bytes so we need to create batches. Since the FPGA receives data in the multiples of 4 bytes we use a batch
        // of 4 bytes. This seems to have no negative impact on performance compared to using serialPort.Write() once.
        const int maxBytesToSendAtOnce = 4;
        var memoryAsArraySegment = memory.GetUnderlyingArray();
        for (int i = 0; i < (int)Math.Ceiling(memory.Length / (decimal)maxBytesToSendAtOnce); i++)
        {
            var remainingBytes = memory.Length - (i * maxBytesToSendAtOnce);
            var bytesToSend = remainingBytes > maxBytesToSendAtOnce ? maxBytesToSendAtOnce : remainingBytes;
            serialPort.Write(memoryAsArraySegment.Array, (i * maxBytesToSendAtOnce) + memoryAsArraySegment.Offset, bytesToSend);
        }

        // Processing the response.
        var serialCommunicationContext = new SerialCommunicationContext
        {
            TaskCompletionSource = new TaskCompletionSource<bool>(),
            CommunicationState = Serial.CommunicationState.WaitForFirstResponse,
            SerialPort = serialPort,
            CommunicationStateContext = context,
        };

        var executionTimeBytes = new ListWithKnownCount<byte>(8);
        var outputByteCountBytes = new ListWithKnownCount<byte>(4);
        var outputBytes = new ListWithKnownCount<byte>(0);

        // In this event we are receiving the useful data coming from the FPGA board.
        serialPort.DataReceived += (_, e) =>
        {
            if (e.EventType != SerialData.Chars) return;

            var inputBuffer = new byte[serialPort.BytesToRead];
            serialPort.Read(inputBuffer, 0, inputBuffer.Length);

            for (int i = 0; i < inputBuffer.Length && serialCommunicationContext.CommunicationState != Serial.CommunicationState.Finished; i++)
            {
                serialCommunicationContext.CommunicationState = ProcessReceivedByte(
                    inputBuffer[i],
                    serialCommunicationContext,
                    executionContext,
                    accessor,
                    executionTimeBytes,
                    outputByteCountBytes,
                    outputBytes);
            }
        };

        await serialCommunicationContext.TaskCompletionSource.Task;

        EndExecution(context);

        return context.HardwareExecutionInformation;
    }

    private Serial.CommunicationState ProcessReceivedByte(
        byte receivedByte,
        SerialCommunicationContext serialCommunicationContext,
        IHardwareExecutionContext executionContext,
        SimpleMemoryAccessor accessor,
        ListWithKnownCount<byte> executionTimeBytes,
        ListWithKnownCount<byte> outputByteCountBytes,
        ListWithKnownCount<byte> outputBytes)
    {
        switch (serialCommunicationContext.CommunicationState)
        {
            case Serial.CommunicationState.WaitForFirstResponse:
                if (receivedByte == Serial.Signals.Ping)
                {
                    serialCommunicationContext.SerialPort.Write(Serial.Signals.Ready);
                }
                else
                {
                    throw new SerialPortCommunicationException(
                        "Awaited a ping signal from the FPGA after it finished but received the following byte instead: " +
                        receivedByte);
                }

                return Serial.CommunicationState.ReceivingExecutionInformation;
            case Serial.CommunicationState.ReceivingExecutionInformation:
                executionTimeBytes.Add(receivedByte);
                if (executionTimeBytes.Count < 8) return Serial.CommunicationState.ReceivingExecutionInformation;

                var executionTimeClockCycles = BitConverter.ToUInt64(executionTimeBytes.Span);
                SetHardwareExecutionTime(
                    serialCommunicationContext.CommunicationStateContext,
                    executionContext,
                    executionTimeClockCycles);
                serialCommunicationContext.SerialPort.Write(Serial.Signals.Ready);

                return Serial.CommunicationState.ReceivingOutputByteCount;
            case Serial.CommunicationState.ReceivingOutputByteCount:
                outputByteCountBytes.Add(receivedByte);
                if (outputByteCountBytes.Count < 4) return Serial.CommunicationState.ReceivingOutputByteCount;

                // Since the output's size can differ from the input size for optimization reasons, we take the explicit
                // size into account.
                outputBytes.Reset(
                    knownCount: BitConverter.ToInt32(outputByteCountBytes.Span),
                    startIndex: MemoryPrefixCellCount * SimpleMemory.MemoryCellSizeBytes);

                Logger.LogInformation("Incoming data size in bytes: {ByteCount}", outputBytes.KnownCount);

                serialCommunicationContext.SerialPort.Write(Serial.Signals.Ready);
                return Serial.CommunicationState.ReceivingOutput;
            case Serial.CommunicationState.ReceivingOutput:
                // There is a padding of PrefixCellCount cells for the unlikely case that the user would directly feed
                // back the output as the next call's input. This way Prefix space is maintained.
                outputBytes.Add(receivedByte);

                if (outputBytes.Count < outputBytes.KnownCount) return Serial.CommunicationState.ReceivingOutput;
                accessor.Set(outputBytes.HandOver(), MemoryPrefixCellCount);

                // Serial communication can give more data than we actually await, so need to set this.
                serialCommunicationContext.SerialPort.Write(Serial.Signals.Ready);
                serialCommunicationContext.TaskCompletionSource.SetResult(true);
                return Serial.CommunicationState.Finished;
            case Serial.CommunicationState.Finished:
                // Nothing to do here.
                return Serial.CommunicationState.Finished;
            default:
                throw new InvalidOperationException(
                    $"Unknown communication state: \"{serialCommunicationContext.CommunicationState}\"");
        }
    }

    /// <summary>
    /// Detects serial-connected compatible FPGA boards.
    /// </summary>
    /// <returns>The serial port name where the FPGA board is connected to.</returns>
    private async Task<IEnumerable<string>> GetFpgaPortNamesAsync(IHardwareExecutionContext executionContext)
    {
        // Get all available serial ports in the system.
        var ports = SerialPort.GetPortNames();

        // If no serial ports were detected, then we can't do anything else.
        if (ports.Length == 0)
        {
            throw new SerialPortCommunicationException(
                "No serial port detected (no serial ports are open). Is an FPGA board connected and is it powered up?");
        }

        var fpgaPortNames = new ConcurrentBag<string>();
        var serialPortPingingTasks = new Task[ports.Length];

        for (int i = 0; i < ports.Length; i++)
        {
            serialPortPingingTasks[i] = Task.Factory.StartNew(
                portNameObject =>
                {
                    using var serialPort = CreateSerialPort(executionContext);
                    var taskCompletionSource = new TaskCompletionSource<bool>();

                    serialPort.DataReceived += (_, _) =>
                    {
                        if (serialPort.ReadByte() == Serial.Signals.Ready)
                        {
                            fpgaPortNames.Add(serialPort.PortName);
                            taskCompletionSource.SetResult(true);
                        }
                    };

                    serialPort.PortName = (string)portNameObject;

                    try
                    {
                        serialPort.Open();
                        serialPort.Write(CommandTypes.WhoIsAvailable);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "IO error while getting port names.");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogInformation(
                            ex,
                            "Couldn't access device while trying to get port names. This may be because the port " +
                            "is used by another application. Checking the next port...");
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // This might happen if a non-FPGA port tries to be opened.
                    }

                    // Waiting a maximum of 3s for a response from the port.
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                    taskCompletionSource.Task.Wait(3000); // This code section is not async.
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
                },
                ports[i],
                default,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }

        await Task.WhenAll(serialPortPingingTasks);

        if (!fpgaPortNames.Any())
        {
            throw new SerialPortCommunicationException(
                "No compatible FPGA board connected to any serial port or a connected FPGA is not answering. Is " +
                "an FPGA board connected and is it powered up? If yes, is the SDK software running on it?");
        }

        return fpgaPortNames;
    }

    private SerialPort CreateSerialPort(IHardwareExecutionContext executionContext)
    {
        var serialPort = new SerialPort
        {
            BaudRate = Serial.DefaultBaudRate,
            Parity = Serial.DefaultParity,
            StopBits = Serial.DefaultStopBits,
            WriteTimeout = Serial.DefaultWriteTimeoutMilliseconds,
        };

        _serialPortConfigurators.InvokePipelineSteps(step => step.ConfigureSerialPort(serialPort, executionContext));

        return serialPort;
    }

    private sealed class SerialCommunicationContext
    {
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
        public Serial.CommunicationState CommunicationState { get; set; }
        public SerialPort SerialPort { get; set; }
        public CommunicationStateContext CommunicationStateContext { get; set; }
    }
}
