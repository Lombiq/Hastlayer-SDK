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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Hast.Communication.Services
{
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

            // Initializing some serial port connection settings (may be different with some FPGA boards).
            // For detailed info on how the SerialPort class works see:
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/e36193cd-a708-42b3-86b7-adff82b19e5e/how-does-serialport-handle-datareceived?forum=netfxbcl
            // Also we might consider this: http://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport

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
                Logger.LogInformation("The port {0} is ours.", serialPort.PortName);
            }
            else
            {
                throw new SerialPortCommunicationException(
                    "Communication with the FPGA board through the serial port failed. The " +
                    serialPort.PortName + " exists but it's used by another process.");
            }

            // Here we put together the data stream.

            // Prepare memory.
            var dma = new SimpleMemoryAccessor(simpleMemory);
            // The first parameter is actually just a byte but we only fetch whole cells so 3/4 of the cell is padding.
            var memory = dma.Get(MemoryPrefixCellCount)[FirstCellPadding..];
            var memoryLength = simpleMemory.ByteCount;

            // Execute Order 66.
            // Set command type
            var commandType = (byte)CommandTypes.Execution;
            MemoryMarshal.Write(memory.Span, ref commandType);
            // Copying the input length, represented as bytes, to the output buffer.
            MemoryMarshal.Write(memory.Span.Slice(1, sizeof(int)), ref memoryLength);
            // Copying the member ID, represented as bytes, to the output buffer.
            MemoryMarshal.Write(memory.Span.Slice(1 + sizeof(int), sizeof(int)), ref memberId);

            // Sending the data.
            // Just using serialPort.Write() once with all the data would stop sending data after 16372 bytes so
            // we need to create batches. Since the FPGA receives data in the multiples of 4 bytes we use a batch
            // of 4 bytes. This seems to have no negative impact on performance compared to using
            // serialPort.Write() once.
            const int maxBytesToSendAtOnce = 4;
            var memoryAsArraySegment = memory.GetUnderlyingArray();
            for (int i = 0; i < (int)Math.Ceiling(memory.Length / (decimal)maxBytesToSendAtOnce); i++)
            {
                var remainingBytes = memory.Length - (i * maxBytesToSendAtOnce);
                var bytesToSend = remainingBytes > maxBytesToSendAtOnce ? maxBytesToSendAtOnce : remainingBytes;
                serialPort.Write(memoryAsArraySegment.Array, (i * maxBytesToSendAtOnce) + memoryAsArraySegment.Offset, bytesToSend);
            }

            // Processing the response.
            await ProcessResponse(dma, serialPort, context, executionContext).Task;

            EndExecution(context);

            return context.HardwareExecutionInformation;
        }

        [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "False positive, it's in the event.")]
        private TaskCompletionSource<bool> ProcessResponse(
            SimpleMemoryAccessor dma,
            SerialPort serialPort,
            CommunicationStateContext context,
            IHardwareExecutionContext executionContext)
        {
            // False positive, probably because the writes are in the event handler.
#pragma warning disable S1854 // Unused assignments should be removed
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var communicationState = Serial.CommunicationState.WaitForFirstResponse;
            var outputByteCountBytes = new byte[4];
            var outputByteCountByteCounter = 0;
            var outputByteCount = 0; // The incoming byte buffer size.
            var outputBytesReceivedCount = 0; // Just used to know when the data is ready.
            var outputBytes = Array.Empty<byte>(); // The incoming buffer.
            var executionTimeBytes = new byte[8];
            var executionTimeByteCounter = 0;
#pragma warning restore S1854 // Unused assignments should be removed

            void ProcessReceivedByte(byte receivedByte)
            {
                switch (communicationState)
                {
                    case Serial.CommunicationState.WaitForFirstResponse:
                        if (receivedByte != Serial.Signals.Ping)
                        {
                            throw new SerialPortCommunicationException(
                                "Awaited a ping signal from the FPGA after it finished but received the following byte instead: " +
                                receivedByte);
                        }

                        communicationState = Serial.CommunicationState.ReceivingExecutionInformation;

                        break;
                    case Serial.CommunicationState.ReceivingExecutionInformation:
                        executionTimeBytes[executionTimeByteCounter++] = receivedByte;
                        communicationState = ReceivedExecutionInformation(
                            executionTimeBytes,
                            executionTimeByteCounter,
                            context,
                            executionContext);

                        break;
                    case Serial.CommunicationState.ReceivingOutputByteCount:
                        outputByteCountBytes[outputByteCountByteCounter++] = receivedByte;

                        if (outputByteCountByteCounter == 4)
                        {
                            outputByteCount = BitConverter.ToInt32(outputByteCountBytes, 0);

                            // Since the output's size can differ from the input size for optimization reasons,
                            // we take the explicit size into account.
                            outputBytes = new byte[outputByteCount + (MemoryPrefixCellCount * SimpleMemory.MemoryCellSizeBytes)];

                            Logger.LogInformation("Incoming data size in bytes: {0}", outputByteCount);

                            communicationState = Serial.CommunicationState.ReceivingOutput;
                        }

                        break;
                    case Serial.CommunicationState.ReceivingOutput:
                        // There is a padding of PrefixCellCount cells for the unlikely case that the user
                        // would directly feed back the output as the next call's input. This way Prefix space
                        // is maintained.
                        outputBytes[outputBytesReceivedCount + (MemoryPrefixCellCount * SimpleMemory.MemoryCellSizeBytes)] = receivedByte;
                        outputBytesReceivedCount++;
                        communicationState = ReceivedOutput(
                            outputBytes,
                            outputByteCount,
                            outputBytesReceivedCount,
                            taskCompletionSource,
                            dma);

                        break;
                    case Serial.CommunicationState.Finished:
                    default:
                        // Intentionally empty.
                        return;
                }

                serialPort.Write(Serial.Signals.Ready);
            }

            // In this event we are receiving the useful data coming from the FPGA board.
            serialPort.DataReceived += (_, e) =>
            {
                if (e.EventType != SerialData.Chars) return;

                var inputBuffer = new byte[serialPort.BytesToRead];
                serialPort.Read(inputBuffer, 0, inputBuffer.Length);

                for (int i = 0; i < inputBuffer.Length && communicationState != Serial.CommunicationState.Finished; i++)
                {
                    ProcessReceivedByte(inputBuffer[i]);
                }
            };

            return taskCompletionSource;
        }

        private static Serial.CommunicationState ReceivedOutput(
            byte[] outputBytes,
            int outputByteCount,
            int outputBytesReceivedCount,
            TaskCompletionSource<bool> taskCompletionSource,
            SimpleMemoryAccessor dma)
        {
            if (outputByteCount != outputBytesReceivedCount) return Serial.CommunicationState.ReceivingOutput;

            dma.Set(outputBytes, MemoryPrefixCellCount);
            taskCompletionSource.SetResult(true);

            // Serial communication can give more data than we actually await, so need to set this.
            return Serial.CommunicationState.Finished;
        }

        private Serial.CommunicationState ReceivedExecutionInformation(
            byte[] executionTimeBytes,
            int executionTimeByteCounter,
            CommunicationStateContext context,
            IHardwareExecutionContext executionContext)
        {
            if (executionTimeByteCounter < 8) return Serial.CommunicationState.ReceivingExecutionInformation;

            var executionTimeClockCycles = BitConverter.ToUInt64(executionTimeBytes, 0);

            SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles);

            return Serial.CommunicationState.ReceivingOutputByteCount;
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
                    portNameObject => RunPort(portNameObject, executionContext, fpgaPortNames),
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

        private void RunPort(object portNameObject, IHardwareExecutionContext executionContext, ConcurrentBag<string> fpgaPortNames)
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
                _logger.LogWarning(ex, "Failed to access serial port");
            }
            catch (UnauthorizedAccessException ex)
            {
                // This happens if the port is used by another app.
                _logger.LogWarning(ex, "Failed to access serial port");
            }

            // Waiting a maximum of 3s for a response from the port.
            taskCompletionSource.Task.Wait(3_000);
        }
    }
}
