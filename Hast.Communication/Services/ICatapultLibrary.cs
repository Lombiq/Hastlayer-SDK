using AdvancedDLSupport;
using IcIWare.NamedIndexers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Catapult = Hast.Communication.Constants.CommunicationConstants.Catapult;

namespace Hast.Communication.Services
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CatapultLogFunction(uint logflag, string pchString);

    [NativeSymbols(Prefix = "FPGA_")]
    public interface ICatapultLibrary : IDisposable
    {
        Catapult.Status GetNumberShellRegisters(UIntPtr fpgaHandle, out uint numShellRegs);
        Catapult.Status ReadShellRegister(UIntPtr fpgaHandle, uint registerNumber, out uint readValue);
        Catapult.Status WriteShellRegister(UIntPtr fpgaHandle, uint registerNumber, uint writeValue);
        Catapult.Status IsGoldenImage(UIntPtr fpgaHandle, out bool isGolden, out uint roleID, out uint roleVersion);
        Catapult.Status ReadRsuImageId(UIntPtr fpgaHandle, out uint pImageId);
        Catapult.Status Reconfig(UIntPtr[] fpgaHandles, uint numHandles, bool reconfigToGolden);
        Catapult.Status WriteFlashImageEx(UIntPtr fpgaHandle, bool writeToGolden, string RpdFilename, uint timeoutInMs = Catapult.DefaultFlashAccessTimeoutInMilliseconds);
        Catapult.Status WriteFlashImage(UIntPtr fpgaHandle, bool writeToGolden, string RpdFilename);
        Catapult.Status CaptureFlashImageEx(UIntPtr fpgaHandle, bool isGolden, string RpdFilename, uint timeoutInMs = Catapult.DefaultFlashAccessTimeoutInMilliseconds);
        Catapult.Status CaptureFlashImage(UIntPtr fpgaHandle, bool isGolden, string RpdFilename);
        Catapult.Status DisableNMI(UIntPtr fpgaHandle);
        Catapult.Status ReadVendorDeviceId(UIntPtr fpgaHandle, out uint vendorId, out uint deviceId);
        Catapult.Status ReviveRootportNoDriver();
        Catapult.Status CycleRootportEnabledNoDriver();
        Catapult.Status CycleDeviceEnabledNoDriver();
        Catapult.Status PrepForReconfig(UIntPtr[] fpgaHandles, uint numHandles);
        Catapult.Status ResetAfterReconfig(UIntPtr[] fpgaHandles, uint numHandles);
        Catapult.Status ReadStatistic(UIntPtr fpgaHandle, uint statType, out uint value);
        Catapult.Status SetNetworkBypass(UIntPtr fpgaHandle, bool forceBypass);
        Catapult.Status TriggerSoftReset(UIntPtr fpgaHandle, uint resetType);
        Catapult.Status SetSlotSize(UIntPtr[] fpgaHandles, uint numHandles, uint bytesPerSlot, uint numSlots);
        Catapult.Status IsDevicePresent(string pchVerManifestFile, [MarshalAs(UnmanagedType.FunctionPtr)]CatapultLogFunction logFunc);
        Catapult.Status CreateHandle(out UIntPtr fpgaHandle, uint endpointNumber, uint flags, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pchVerDefnsFile, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pchVersionManifestFile, [MarshalAs(UnmanagedType.FunctionPtr)]CatapultLogFunction logFunc = null);
        Catapult.Status GetVersion(UIntPtr fpgaHandle, string pchVerKey, out uint pVersion);
        Catapult.Status CloseHandle(UIntPtr fpgaHandle);
        Catapult.Status GetNumberEndpoints(UIntPtr fpgaHandle, out uint numEndpoints);
        Catapult.Status GetNumberBuffers(UIntPtr fpgaHandle, out uint numBuffers);
        Catapult.Status GetBufferSize(UIntPtr fpgaHandle, out uint bufferBytes);
        Catapult.Status GetLastError();
        Catapult.Status GetLastErrorText([MarshalAs(UnmanagedType.LPStr)]StringBuilder pErrorTextBuf, int cbBufSize);
        Catapult.Status SetLastErrorText([MarshalAs(UnmanagedType.LPStr)]StringBuilder pErrorTextBuf);
        Catapult.Status GetInputBufferPointer(UIntPtr fpgaHandle, uint whichBuffer, out UIntPtr inputBufferPtr);
        Catapult.Status GetOutputBufferPointer(UIntPtr fpgaHandle, uint whichBuffer, out UIntPtr outputBufferPtr);
        Catapult.Status GetResultBufferPointer(UIntPtr fpgaHandle, uint whichBuffer, out UIntPtr resultBufferPtr);
        Catapult.Status ReadSoftRegister(UIntPtr fpgaHandle, uint address, out ulong readValue);
        Catapult.Status WriteSoftRegister(UIntPtr fpgaHandle, uint address, ulong writeValue);
        Catapult.Status GetInputBufferFull(UIntPtr fpgaHandle, uint whichBuffer, out bool isFull);
        Catapult.Status GetOutputBufferDone(UIntPtr fpgaHandle, uint whichBuffer, out bool isDone);
        Catapult.Status SendInputBuffer(UIntPtr fpgaHandle, uint whichBuffer, uint sizeBytes, bool useInterrupt = true);
        Catapult.Status WaitOutputBuffer(UIntPtr fpgaHandle, uint whichBuffer, out uint pBytesReceived, bool useInterrupt = true, double timeoutInSeconds = Catapult.WaitOutputBufferTimeoutDefaultSeconds);
        Catapult.Status DiscardOutputBuffer(UIntPtr fpgaHandle, uint whichBuffer);
    }


    [Serializable]
    public class CatapultFunctionResultException : Exception
    {
        public Catapult.Status Status { get; set; }

        public CatapultFunctionResultException() { }
        public CatapultFunctionResultException(Catapult.Status status, string message)
            : this(String.IsNullOrWhiteSpace(message) ? status.ToString() : message) { Status = status; }
        public CatapultFunctionResultException(string message) : base(message) { }
        public CatapultFunctionResultException(string message, Exception inner) : base(message, inner) { }
        protected CatapultFunctionResultException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }

    public class CatapultLibrary : IDisposable
    {
        private bool _isDisposed = false;
        private UIntPtr _handle;

        public ICatapultLibrary Native { get; private set; }
        public UIntPtr Handle { get { return _handle; } }

        public string VersionDefinitionsFile { get; private set; }
        public string VersionManifestFile { get; private set; }
        public CatapultLogFunction LogFunction { get; private set; }

        /// <summary>
        /// Gets or sets the value of the Soft Register. Indexed by memory address.
        /// </summary>
        public readonly NamedIndexer<uint, ulong> SoftRegister;

        /// <summary>
        /// Gets or sets the value of the Shell Register. Indexed by register number.
        /// </summary>
        public readonly NamedIndexer<uint, uint> ShellRegister;

        public CatapultLibrary(string libraryPath, string versionDefinitionsFile = null, string versionManifestFile = null, CatapultLogFunction logFunction = null)
        {
            VersionDefinitionsFile = versionDefinitionsFile;
            VersionManifestFile = versionManifestFile;
            LogFunction = logFunction;

            SoftRegister = new NamedIndexer<uint, ulong>(
                (address) => { ulong value; VerifyResult(Native.ReadSoftRegister(_handle, address, out value)); return value; },
                (address, value) => { VerifyResult(Native.WriteSoftRegister(_handle, address, value)); }
            );

            ShellRegister = new NamedIndexer<uint, uint>(
                (registerNumber) => { uint value; VerifyResult(Native.ReadShellRegister(_handle, registerNumber, out value)); return value; },
                (registerNumber, value) => { VerifyResult(Native.WriteShellRegister(_handle, registerNumber, value)); }
            );

            Native = NativeLibraryBuilder.Default.ActivateInterface<ICatapultLibrary>(libraryPath);

            var create_status = Native.CreateHandle(out _handle,
                Catapult.PcieHipNumber,
                0,
                String.IsNullOrEmpty(versionDefinitionsFile) ? null : new StringBuilder(versionDefinitionsFile),
                String.IsNullOrEmpty(versionManifestFile) ? null : new StringBuilder(versionManifestFile),
                logFunction);
            VerifyResult(create_status);

            // Configure hardware settings & enable hardware
            SoftRegister[Catapult.ConfigDram.Channel0] = 0;
            SetPcieEnabled(true);
        }

        /// <summary>
        /// Verifies the result of an ICatapultLibrary function call. If the result is SUCCESS then nothing happens.
        /// Otherwise CatapultFunctionResultException is thrown with the error message form the library.
        /// </summary>
        /// <param name="status">The return value from the library function.</param>
        /// <exception cref="CatapultFunctionResultException">This exception is thrown when the status isn't SUCCESS.</exception>
        public void VerifyResult(Catapult.Status status)
        {
            if (status == Catapult.Status.Success)
                return;

            var errorMessage = new StringBuilder(512);
            Native.GetLastError();
            Native.GetLastErrorText(errorMessage, errorMessage.Capacity);

            throw new CatapultFunctionResultException(status, errorMessage.ToString());
        }

        public void SetPcieEnabled(bool enable)
        {
            uint pcie;
            VerifyResult(Native.ReadShellRegister(_handle, 0, out pcie));

            // set control_register[6]
            if (enable)
                pcie |= Catapult.RegisterMaskPcieEnabled;
            else
                pcie &= ~Catapult.RegisterMaskPcieEnabled;

            VerifyResult(Native.WriteShellRegister(_handle, 0, pcie));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            SetPcieEnabled(false);
            Native.Dispose();
        }
    }
}
