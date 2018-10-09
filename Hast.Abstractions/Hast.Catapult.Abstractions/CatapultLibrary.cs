using AdvancedDLSupport;
using Hast.Communication.Exceptions;
using IcIWare.NamedIndexers;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Catapult.Abstractions
{
    /// <summary>
    /// Job and configuration manager for the Catapult FPGA driver.
    /// </summary>
    public class CatapultLibrary : IDisposable
    {
        private bool _isDisposed = false;
        private IntPtr _handle;

        /// <summary>
        /// Contains the latest tasks to be awaited when starting a new task.
        /// </summary>
        private readonly ConcurrentQueue<Task>[] _slotDispatch;

        /// <summary>
        /// Gets the interface containing the functions exposed by the native FPGA library.
        /// </summary>
        public ICatapultNativeLibrary NativeLibrary { get; private set; }

        /// <summary>
        /// Gets the device handle utilized by the functions in NativeLibrary.
        /// </summary>
        public IntPtr Handle { get => _handle; }

        /// <summary>
        /// Gets the function used for logging. If null, no logging is done.
        /// </summary>
        public CatapultLogFunction LogFunction { get; private set; }


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
        /// Gets the maximum length of the message sent to the input buffer. (64kB)
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// Gets whether the PCIe access is enabled on the device.
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

        /// <summary>
        /// Gets or sets the value of the Soft Register. Indexed by memory address.
        /// </summary>
        public readonly NamedIndexer<uint, ulong> SoftRegister;

        /// <summary>
        /// Gets or sets the value of the Shell Register. Indexed by register number.
        /// </summary>
        public readonly NamedIndexer<uint, uint> ShellRegister;

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
        public CatapultLibrary(string libraryPath = Constants.DefaultLibraryPath,
            string versionDefinitionsFile = null,
            string versionManifestFile = null,
            CatapultLogFunction logFunction = null)
        {
            LogFunction = logFunction;

            // Indexer register access
            SoftRegister = new NamedIndexer<uint, ulong>(
                (address) => { VerifyResult(NativeLibrary.ReadSoftRegister(_handle, address, out ulong value)); return value; },
                (address, value) => { VerifyResult(NativeLibrary.WriteSoftRegister(_handle, address, value)); }
            );
            ShellRegister = new NamedIndexer<uint, uint>(
                (register) => { VerifyResult(NativeLibrary.ReadShellRegister(_handle, register, out uint value)); return value; },
                (register, value) => { VerifyResult(NativeLibrary.WriteShellRegister(_handle, register, value)); }
            );

            // Initialize FPGA library and device
            if (!File.Exists(libraryPath + ".dll") && Directory.Exists(@"E:\catapult\v1.2\Driver\Bin"))
            {
                File.Copy(string.Format(@"E:\catapult\v1.2\Driver\Bin\{0}.dll", libraryPath), libraryPath + ".dll");
                File.Copy(@"E:\catapult\v1.2\Driver\Bin\FPGADefaultVersionManifest.ini", "FPGADefaultVersionManifest.ini");
                File.Copy(@"E:\catapult\v1.2\Driver\Bin\FPGAVersionDefinitions.ini", "FPGAVersionDefinitions.ini");
            }
            NativeLibrary = NativeLibraryBuilder.Default.ActivateInterface<ICatapultNativeLibrary>(libraryPath);
            VerifyResult(NativeLibrary.IsDevicePresent(versionManifestFile, logFunction));
            var createStatus = NativeLibrary.CreateHandle(out _handle,
                endpointNumber: Constants.PcieHipNumber,
                flags: 0,
                pchVerDefnsFile: string.IsNullOrEmpty(versionDefinitionsFile) ? null : new StringBuilder(versionDefinitionsFile),
                pchVersionManifestFile: string.IsNullOrEmpty(versionManifestFile) ? null : new StringBuilder(versionManifestFile),
                logFunc: logFunction);
            VerifyResult(createStatus);

            // Configure hardware settings & enable hardware
            SoftRegister[Constants.ConfigDram.Channel0] = 0;
            PcieEnabled = true;

            // Query device info
            VerifyResult(NativeLibrary.GetNumberShellRegisters(_handle, out uint count));
            ShellRegisterCount = (int)count;
            VerifyResult(NativeLibrary.GetNumberBuffers(_handle, out count));
            BufferCount = (int)count;
            VerifyResult(NativeLibrary.GetBufferSize(_handle, out count));
            BufferSize = (int)count;

            // Set up the task-based per-slot queuing mechanism
            _slotDispatch = new ConcurrentQueue<Task>[BufferCount];
            for (int i = 0; i < BufferCount; i++)
                _slotDispatch[i] = new ConcurrentQueue<Task>();
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
            if (_isDisposed) return;

            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), "Closing down the FPGA...\n");
            PcieEnabled = false;
            NativeLibrary.CloseHandle(Handle);

            NativeLibrary.Dispose();
            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), "Closed down the FPGA...\n");

            _isDisposed = true;
        }


        /// <summary>
        /// Verifies the result of an ICatapultLibrary function call. If the result is SUCCESS or WAIT_TIMEOUT then
        /// nothing happens. Otherwise CatapultFunctionResultException is thrown with the error message form the library.
        /// </summary>
        /// <param name="status">The return value from the library function.</param>
        /// <exception cref="CatapultFunctionResultException">Thrown when the status isn't SUCCESS.</exception>
        public void VerifyResult(Constants.Status status)
        {
            if (status == Constants.Status.Success || status == Constants.Status.WaitTimeout) return;

            var errorMessage = new StringBuilder(512);
            NativeLibrary.GetLastError();
            NativeLibrary.GetLastErrorText(errorMessage, errorMessage.Capacity);

            throw new CatapultFunctionResultException(status, errorMessage.ToString());
        }


        /// <summary>
        /// Uploads the data to the selected slot's input buffer and awaits the output.
        /// But first it checks if there are any other jobs in queue and awaits them if there are any.
        /// </summary>
        /// <param name="slotId">The numeric ID of the slot that contains the called program. (0 - BufferCount)</param>
        /// <param name="inputData">The hardware program's input.</param>
        /// <returns>The resulting output from the FPGA.</returns>
        public async Task<byte[]> ExecuteJob(int slotId, byte[] inputData)
        {
            // This job will contain the current call
            Task<byte[]> job = null;

            lock (_slotDispatch[slotId])
            {
                // Check if there was a previous job we have to await
                if (!_slotDispatch[slotId].TryDequeue(out Task previousJob))
                    previousJob = Task.CompletedTask;
                job = previousJob.ContinueWith(_ => { return RunJob(slotId, inputData).Result; });
                _slotDispatch[slotId].Enqueue(job);
            }

            return await job;
        }

        /// <summary>
        /// Uploads the data to the selected slot's input buffer and awaits the output.
        /// </summary>
        /// <param name="slotId">The numeric ID of the slot that contains the called program. (0 - BufferCount)</param>
        /// <param name="inputData">The hardware program's input.</param>
        /// <returns>The resulting output from the FPGA.</returns>
        private async Task<byte[]> RunJob(int slotId, byte[] inputData)
        {
            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), $"Job on slot #{slotId} starting...\n");
            Debug.Assert(slotId < BufferCount);
            Debug.Assert(inputData.Length <= BufferSize);
            var slot = (uint)slotId;

            // Make sure the buffer is ready to be written
            bool isInputBufferFull;
            do
            {
                VerifyResult(NativeLibrary.GetInputBufferFull(
                    _handle, slot, out isInputBufferFull));
                if (isInputBufferFull) await Task.Delay(1);
            } while (isInputBufferFull);

            // If the input message is too short, pad it with zeros
            if (inputData.Length < Constants.BufferMessageSizeMinByte)
                Array.Resize<byte>(ref inputData, Constants.BufferMessageSizeMinByte);

            // If the input message isn't 16B aligned, pad it with zeros
            if (inputData.Length % 16 != 0)
            {
                int paddedLength = (int)Math.Ceiling(inputData.Length / 16.0) * 16;
                Array.Resize<byte>(ref inputData, paddedLength);
            }

            // Acquire I/O buffer addresses
            VerifyResult(NativeLibrary.GetInputBufferPointer(_handle, slot, out IntPtr inputBuffer));
            VerifyResult(NativeLibrary.GetOutputBufferPointer(_handle, slot, out IntPtr outputBuffer));

            LogFunction?.Invoke((uint)Constants.Log.Debug,
                $"Buffer #{slot} @ {inputBuffer.ToInt64()} input start: {Marshal.PtrToStructure<byte>(inputBuffer)}\n");

            // Upload data into input buffer
            Marshal.Copy(inputData, 0, inputBuffer, inputData.Length);
            VerifyResult(NativeLibrary.SendInputBuffer(_handle, slot, (uint)inputData.Length));

            // Wait for the interrupt and download results from output buffer
            var resultSize = await Task.Run(() =>
            {
                VerifyResult(NativeLibrary.WaitOutputBuffer(_handle, slot, out uint bytesReceived));
                return (int)bytesReceived;
            });
            Debug.Assert(resultSize == inputData.Length);
            var result = new byte[resultSize];
            Marshal.Copy(outputBuffer, result, 0, resultSize);

            LogFunction?.Invoke((uint)Constants.Log.Debug,
                $"Buffer #{slot} @ {outputBuffer.ToInt64()} output start: {Marshal.PtrToStructure<byte>(outputBuffer)}\n");

            // Signal that we are done
            NativeLibrary.DiscardOutputBuffer(_handle, slot);

            LogFunction?.Invoke((uint)(Constants.Log.Info | Constants.Log.Verbose), $"Job on slot #{slotId} finished!\n");
            return result;
        }
    }
}
