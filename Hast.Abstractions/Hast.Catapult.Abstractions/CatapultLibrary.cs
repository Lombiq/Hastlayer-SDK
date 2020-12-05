using AdvancedDLSupport;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using InputHeaderSizes = Hast.Catapult.Abstractions.Constants.InputHeaderSizes;
using OutputHeaderSizes = Hast.Catapult.Abstractions.Constants.OutputHeaderSizes;

namespace Hast.Catapult.Abstractions
{
    /// <summary>
    /// Job and configuration manager for the Catapult FPGA driver.
    /// </summary>
    public sealed class CatapultLibrary : IDisposable
    {
        private bool _isDisposed = false;
        private readonly IntPtr _handle;

        /// <summary>
        /// Gets the name of the instance (Catapult:N where N is the PCIe endpoint number).
        /// </summary>
        public string InstanceName => $"{Constants.ChannelName}:{PcieEndpointNumber}";

        /// <summary>
        /// Contains the latest tasks to be awaited when starting a new task.
        /// </summary>
        private readonly Task[] _slotDispatch;

        /// <summary>
        /// The slot to be used for the next run.
        /// </summary>
        private int _currentSlot;

        /// <summary>
        /// Gets the interface containing the functions exposed by the native FPGA library.
        /// </summary>
        public ICatapultNativeLibrary NativeLibrary { get; private set; }

        /// <summary>
        /// Gets the device handle utilized by the functions in NativeLibrary.
        /// </summary>
        public IntPtr Handle => _handle;

        /// <summary>
        /// Gets the function used for logging. If null, no logging is done.
        /// </summary>
        public CatapultLogFunction LogFunction { get; private set; }

        /// <summary>
        /// Gets the numeric ID of the PCIe endpoint (physical slot).
        /// </summary>
        public int PcieEndpointNumber { get; private set; }

        /// <summary>
        /// Gets the total number of shell registers.
        /// </summary>
        public int ShellRegisterCount { get; private set; }

        /// <summary>
        /// Gets the number of buffers. When selecting an input buffer the slot index must be between 0 and
        /// (InputBufferCount - 1) inclusive.
        /// </summary>
        public int BufferCount { get; private set; }

        /// <summary>
        /// Gets the maximum length of the message sent to the input buffer (64kB).
        /// </summary>
        public int BufferSize
        {
            get => BufferPayloadSize + InputHeaderSizes.Total;
            private set => BufferPayloadSize = value - InputHeaderSizes.Total;
        }

        /// <summary>
        /// Gets the amount of space available for useful data in the input buffer.
        /// </summary>
        public int BufferPayloadSize { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the PCIe access is enabled on the device.
        /// </summary>
        public bool PcieEnabled
        {
            get
            {
                VerifyResult(NativeLibrary.ReadShellRegister(_handle, 0, out uint pcie));
                return (pcie & Constants.RegisterMaskPcieEnabled) != 0;
            }
            private set
            {
                VerifyResult(NativeLibrary.ReadShellRegister(_handle, 0, out uint pcie));

                // Set control_register[6]
                if (value) pcie |= Constants.RegisterMaskPcieEnabled;
                else pcie &= ~Constants.RegisterMaskPcieEnabled;

                VerifyResult(NativeLibrary.WriteShellRegister(_handle, 0, pcie));
            }
        }

        public System.IO.TextWriter TesterOutput { get; set; }

        /// <summary>
        /// Initializes a new instance of the CatapultLibrary class.
        /// </summary>
        /// <param name="libraryPath">The path of the FPGACoreLib DLL file without the extension.</param>
        /// <param name="versionDefinitionsFile">
        /// The location of the version definitions file. If left as null, then "FPGAVersionDefinitions.ini" is expected
        /// in the current working directory.
        /// </param>
        /// <param name="versionManifestFile">
        /// The location of the version manifest file. If left as null, then "FPGADefaultVersionManifest.ini" is expected
        /// in the current working directory.
        /// </param>
        /// <param name="logFunction">
        /// Optional logging function that takes flag values from Hast.Communication.Constants.Constants.Log as its first
        /// parameter and a string as its second. Note that the strings all end with newline character.
        /// </param>
        public CatapultLibrary(
            string libraryPath = Constants.DefaultLibraryPath,
            string versionDefinitionsFile = null,
            string versionManifestFile = null,
            int endpointNumber = Constants.PcieHipNumber,
            CatapultLogFunction logFunction = null)
        {
            LogFunction = logFunction;
            PcieEndpointNumber = endpointNumber;

            // Initialize FPGA library and device.
            // We don't use dllmap so create ALD without it.
            var builderWithoutDllMap = new NativeLibraryBuilder(NativeLibraryBuilder.Default.Options & ~ImplementationOptions.EnableDllMapSupport);
            NativeLibrary = builderWithoutDllMap.ActivateInterface<ICatapultNativeLibrary>(libraryPath);
            // Check if device is available and connect
            VerifyResult(NativeLibrary.IsDevicePresent(versionManifestFile, logFunction));
            var createStatus = NativeLibrary.CreateHandle(
                out _handle,
                endpointNumber: (uint)endpointNumber,
                flags: 0,
                pchVerDefnsFile: string.IsNullOrEmpty(versionDefinitionsFile) ? null : new StringBuilder(versionDefinitionsFile),
                pchVersionManifestFile: string.IsNullOrEmpty(versionManifestFile) ? null : new StringBuilder(versionManifestFile),
                logFunc: logFunction);
            VerifyResult(createStatus);

            // Configure hardware settings & enable hardware.
            SetSoftRegister(Constants.ConfigDram.Channel0, 0);
            PcieEnabled = true;

            // Query device info.
            VerifyResult(NativeLibrary.GetNumberShellRegisters(_handle, out uint count));
            ShellRegisterCount = (int)count;
            VerifyResult(NativeLibrary.GetNumberBuffers(_handle, out count));
            BufferCount = (int)count;
            VerifyResult(NativeLibrary.GetBufferSize(_handle, out count));
            BufferSize = (int)count;

            // Load in configuration from the soft registers
            var allowedSlots = (int)GetSoftRegister(Constants.SoftRegisters.AllowedSlots);
            if (0 < allowedSlots && allowedSlots < BufferCount) BufferCount = allowedSlots;
            var bufferPayloadSize = (int)GetSoftRegister(Constants.SoftRegisters.BufferPayloadSize);
            if (0 < bufferPayloadSize && bufferPayloadSize < BufferPayloadSize) BufferPayloadSize = bufferPayloadSize;

            // Set up the task tracker.
            _slotDispatch = new Task[BufferCount];
            for (int i = 0; i < BufferCount; i++) _slotDispatch[i] = Task.CompletedTask;
        }

        public static CatapultLibrary Create(IDictionary<string, object> config, ILogger<CatapultLibrary> logger, int endpointNumber = Constants.PcieHipNumber)
        {
            var libraryPath = config.ContainsKey(Constants.ConfigKeys.LibraryPath) ?
                config[Constants.ConfigKeys.LibraryPath] ?? Constants.DefaultLibraryPath :
                Constants.DefaultLibraryPath;
            var versionDefinitionsFile = config.ContainsKey(Constants.ConfigKeys.VersionDefinitionsFile) ?
                config[Constants.ConfigKeys.VersionDefinitionsFile] : null;
            var versionManifestFile = config.ContainsKey(Constants.ConfigKeys.VersionManifestFile) ?
                config[Constants.ConfigKeys.VersionManifestFile] : null;

            return new CatapultLibrary(
                (string)libraryPath,
                (string)versionDefinitionsFile,
                (string)versionManifestFile,
                endpointNumber: endpointNumber,
                logFunction: (flagValue, text) =>
                {
                    var flag = (Constants.Log)flagValue;
                    if (flag == Constants.Log.None) return;

                    if (flag.HasFlag(Constants.Log.Debug) || flag.HasFlag(Constants.Log.Verbose))
                        logger.LogDebug(text);
                    else if (flag.HasFlag(Constants.Log.Info))
                        logger.LogInformation(text);
                    else if (flag.HasFlag(Constants.Log.Error))
                        logger.LogError(text);
                    else if (flag.HasFlag(Constants.Log.Fatal))
                        logger.LogCritical(text);
                    else if (flag.HasFlag(Constants.Log.Warn))
                        logger.LogWarning(text);
                });
        }

        /// <summary>
        /// Finalizer to ensure the instance is disposed at some point after the end of its lifecycle.
        /// </summary>
        ~CatapultLibrary() => Dispose();

        /// <summary>
        /// Turns off PCIe, cleans up the access handle and the library.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed || Handle == IntPtr.Zero) return;

            try { WaitClean(); } catch { }

            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), "Closing down the FPGA..." + Environment.NewLine);
            PcieEnabled = false;
            NativeLibrary.CloseHandle(Handle);

            NativeLibrary.Dispose();
            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), "Closed down the FPGA..." + Environment.NewLine);

            _isDisposed = true;
        }

        /// <summary>
        /// Waits until all jobs are concluded and then resets the slot counter so AssignJob can start with a clean slate.
        /// </summary>
        public void WaitClean()
        {
            Task.WaitAll(_slotDispatch);
            _currentSlot = -1;
        }

        /// <summary>
        /// Verifies the result of an ICatapultLibrary function call. If the result is SUCCESS or WAIT_TIMEOUT then
        /// nothing happens. Otherwise CatapultFunctionResultException is thrown with the error message form the library.
        /// </summary>
        /// <param name="status">The return value from the library function.</param>
        /// <exception cref="CatapultFunctionResultException">Thrown when the status isn't SUCCESS.</exception>
        public void VerifyResult(Constants.Status status)
        {
            if (status == Constants.Status.Success) return;

            var errorMessage = new StringBuilder(512);
            NativeLibrary.GetLastError();
            NativeLibrary.GetLastErrorText(errorMessage, errorMessage.Capacity);

            throw new CatapultFunctionResultException(status, errorMessage.ToString());
        }

        /// <summary>
        /// Gets the value of the Soft Register. Indexed by memory address.
        /// </summary>
        /// <param name="address">The memory address.</param>
        /// <returns>The value of the soft register.</returns>
        public ulong GetSoftRegister(uint address)
        {
            VerifyResult(NativeLibrary.ReadSoftRegister(_handle, address, out ulong value)); return value;
        }

        /// <summary>
        /// Sets the value of the Soft Register. Indexed by memory address.
        /// </summary>
        /// <param name="address">The memory address.</param>
        /// <param name="value">The new value.</param>
        public void SetSoftRegister(uint address, ulong value) => VerifyResult(NativeLibrary.WriteSoftRegister(_handle, address, value));

        /// <summary>
        /// Uploads the data to the selected slot's input buffer and awaits the output.
        /// But first it checks if there are any other jobs in queue and awaits them if there are any.
        /// </summary>
        /// <param name="memberId">Identifies the program on the hardware.</param>
        /// <param name="inputData">The hardware program's input.</param>
        /// <returns>The resulting output from the FPGA.</returns>
        public Task<Memory<byte>> AssignJob(int memberId, Memory<byte> inputData) =>
            AssignJob(memberId, inputData, 0, 1, -1, false);

        private async Task<Memory<byte>> AssignJob(
            int memberId,
            Memory<byte> inputData,
            int sliceIndex,
            int sliceCountValue,
            int totalDataSize,
            bool ignoreResponse)
        {
            int currentSliceCount = (int)Math.Ceiling(((double)inputData.Length) / BufferPayloadSize);
            if (totalDataSize < 0) totalDataSize = inputData.Length / SimpleMemory.MemoryCellSizeBytes;

            if (currentSliceCount >= 2)
            {
                TesterOutput?.WriteLine("Slices: {0}", currentSliceCount);
                bool slotOverflow = currentSliceCount > BufferCount;

                // If the data exceeds buffer size limit, then cut it into maximal slices.
                var tasks = new List<Task<Memory<byte>>>(currentSliceCount);
                for (int i = 0; i < currentSliceCount; i++)
                {
                    var slice = i < currentSliceCount - 1 ? inputData.Slice(i * BufferPayloadSize, BufferPayloadSize) :
                        inputData[(tasks.Count * BufferPayloadSize)..];
                    tasks.Add(AssignJob(
                        memberId,
                        slice,
                        i,
                        currentSliceCount,
                        totalDataSize,
                        slotOverflow));
                }

                if (slotOverflow)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                    for (int i = 0; i < currentSliceCount; i++)
                    {
                        tasks.Add(ReceiveJobResults((uint)(i % BufferCount)));
                        if ((i + 1) % BufferCount == 0) await Task.WhenAll(tasks);
                    }
;
                }

                // Make sure that all slots are done.
                var responses = await Task.WhenAll(tasks);

                // Create output array and fill it with the responses that have a positive slice index.
                var payloadTotalCells = MemoryMarshal.Read<int>(responses[0][OutputHeaderSizes.HardwareExecutionTime..].Span);
                Memory<byte> result = new byte[payloadTotalCells * SimpleMemory.MemoryCellSizeBytes + OutputHeaderSizes.Total];
                responses[0].Slice(0, OutputHeaderSizes.Total).CopyTo(result);
                const int sliceIndexPosition = OutputHeaderSizes.HardwareExecutionTime + OutputHeaderSizes.PayloadLengthCells;
                Parallel.For(0, responses.Length, (i) =>
                {
                    int size = i < responses.Length - 1 ? BufferPayloadSize :
                        (payloadTotalCells * SimpleMemory.MemoryCellSizeBytes) - (i * BufferPayloadSize);
                    var offset = BufferPayloadSize * MemoryMarshal.Read<int>(responses[i].Span[sliceIndexPosition..]);
                    if (offset < 0 || offset >= result.Length) return;

                    var targetSlice = result[(OutputHeaderSizes.Total + offset)..];
                    responses[i].Slice(OutputHeaderSizes.Total, size).CopyTo(targetSlice);
                });

                return result;
            }

            Memory<byte> data = new byte[InputHeaderSizes.Total + inputData.Length];
            MemoryMarshal.Write(data.Span, ref memberId);
            MemoryMarshal.Write(data.Span[InputHeaderSizes.MemberId..], ref totalDataSize);
            MemoryMarshal.Write(data.Span[(InputHeaderSizes.MemberId + InputHeaderSizes.PayloadLengthCells)..], ref sliceIndex);
            MemoryMarshal.Write(
                data.Span[(InputHeaderSizes.MemberId + InputHeaderSizes.PayloadLengthCells
                + InputHeaderSizes.SliceIndex)..], ref sliceCountValue);
            inputData.CopyTo(data[InputHeaderSizes.Total..]);

            // This job will contain the current call.
            Task<Memory<byte>> job = null;

            int currentSlot;
            lock (_slotDispatch)
            {
                // Go round-robin,
                _currentSlot = (_currentSlot + 1) % BufferCount;

                currentSlot = _currentSlot;

                // We are inside a lock.
#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                _slotDispatch[currentSlot] = job = _slotDispatch[currentSlot]
                    .ThenAsync(() => RunJob(currentSlot, data, ignoreResponse).Result);
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method
            }

            var jobResult = await job;
            if (ignoreResponse)
                TesterOutput?.WriteLine("Job Finished{0}************{0}Slot: {1}{0}", Environment.NewLine, currentSlot);
            else
                TesterOutput?.WriteLine(
                    "Job Finished{0}************{0}Slot: {1}{0}Time: {2}{0}Payload: {3} cells{0}Slice: {4}{0}",
                    Environment.NewLine,
                    currentSlot,
                    MemoryMarshal.Read<long>(jobResult.Span),
                    MemoryMarshal.Read<int>(jobResult[OutputHeaderSizes.HardwareExecutionTime..].Span),
                    MemoryMarshal.Read<int>(jobResult[(OutputHeaderSizes.HardwareExecutionTime + OutputHeaderSizes.PayloadLengthCells)..].Span));
            return jobResult;
        }

        /// <summary>
        /// Uploads the data to the selected slot's input buffer and awaits the output.
        /// </summary>
        /// <param name="bufferIndex">
        /// The numeric ID of the buffer that the hardware uses for interaction. It is called "slot" in the Mt Granite
        /// Shell Architectural Specification and its value can range from 0 to BufferCount.
        /// </param>
        /// <param name="inputData">The hardware program's input.</param>
        /// <param name="ignoreResponse">If true, the scheduler won't wait for the output to appear.</param>
        /// <returns>The resulting output from the FPGA.</returns>
        private async Task<Memory<byte>> RunJob(int bufferIndex, Memory<byte> inputData, bool ignoreResponse)
        {
            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), $"Job on slot #{bufferIndex} starting...{Environment.NewLine}");
            Debug.Assert(bufferIndex < BufferCount);
            Debug.Assert(inputData.Length <= BufferSize);
            var slot = (uint)bufferIndex;

            // Makes sure the buffer is ready to be written.
            bool isInputBufferFull;
            do
            {
                VerifyResult(NativeLibrary.GetInputBufferFull(
                    _handle, slot, out isInputBufferFull));
                // While unlikely, it's possible that the device has provided a result (so the previous Task is
                // completed), but the input buffer hasn't cleared yet. In this case we should wait for it.
                if (isInputBufferFull) await Task.Delay(1);
            } while (isInputBufferFull);

            // If the input message isn't 64B aligned, pad it with zeros.
            var payloadBytes = inputData.Length - InputHeaderSizes.Total;
            if (payloadBytes % Constants.BufferChunkBytes != 0)
            {
                int paddedPayloadSize = Constants.BufferChunkBytes * (payloadBytes / Constants.BufferChunkBytes + 1);
                if (paddedPayloadSize <= BufferPayloadSize)
                {
                    LogFunction((uint)Constants.Log.Warn, $"Incoming payload ({payloadBytes}B) must be aligned to " +
                        $"{Constants.BufferChunkBytes}B! Padding for {Constants.BufferChunkBytes - paddedPayloadSize}B...");
                    var padded = new byte[paddedPayloadSize + InputHeaderSizes.Total];
                    inputData.CopyTo(padded);
                    inputData = padded;
                }
            }

            VerifyResult(NativeLibrary.GetInputBufferPointer(_handle, slot, out var inputBuffer));

            LogFunction?.Invoke(
                (uint)Constants.Log.Debug,
                $"Buffer #{slot} @ {inputBuffer.ToInt64()} input start: {Marshal.PtrToStructure<byte>(inputBuffer)}{Environment.NewLine}");

            // Upload data into input buffer.
            unsafe
            {
                inputData.Span.CopyTo(new Span<byte>(inputBuffer.ToPointer(), inputData.Length));
            }

            VerifyResult(NativeLibrary.SendInputBuffer(_handle, slot, (uint)inputData.Length));

            TesterOutput?.WriteLine("Slot: {0}, input length: {1} bytes", slot, inputData.Length);
            var resultSizeAcknowledge = await Task.Run(() =>
            {
                VerifyResult(NativeLibrary.WaitOutputBuffer(_handle, slot, out uint bytesReceived));
                NativeLibrary.DiscardOutputBuffer(_handle, slot);
                return (int)bytesReceived;
            });
            TesterOutput?.WriteLine("Slot: {0}, input length: {1} bytes, ACK bytes: {2}", slot, inputData.Length, resultSizeAcknowledge);

            return ignoreResponse ? null : await ReceiveJobResults(slot);
        }

        private async Task<Memory<byte>> ReceiveJobResults(uint slot)
        {
            // Wait for the interrupt and download results from output buffer.
            VerifyResult(NativeLibrary.GetOutputBufferPointer(_handle, slot, out var outputBuffer));
            var resultSize = await Task.Run(() =>
            {
                VerifyResult(NativeLibrary.WaitOutputBuffer(_handle, slot, out uint bytesReceived,
                    useInterrupt: true, timeoutInSeconds: int.MaxValue));
                return (int)bytesReceived;
            });

            Memory<byte> resultMemory = new byte[resultSize];
            unsafe
            {
                new Span<byte>(outputBuffer.ToPointer(), resultSize).CopyTo(resultMemory.Span);
            }

            LogFunction?.Invoke(
                (uint)Constants.Log.Debug,
                $"Buffer #{slot} @ {outputBuffer.ToInt64()} output start: {Marshal.PtrToStructure<byte>(outputBuffer)}{Environment.NewLine}");

            // Signal that we are done.
            NativeLibrary.DiscardOutputBuffer(_handle, slot);

            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), $"Job on slot #{slot} finished!{Environment.NewLine}");
            return resultMemory;
        }

        public override string ToString() => InstanceName;
    }
}
