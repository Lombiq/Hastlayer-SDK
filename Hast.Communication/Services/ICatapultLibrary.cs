using AdvancedDLSupport;
using System;
using System.Runtime.InteropServices;
using System.Text;

using Catapult = Hast.Communication.Constants.CommunicationConstants.Catapult;


namespace Hast.Communication.Services
{
    /// <summary>
    /// The function used by logging in FpgaCoreLib.dll and the CatapultLibrary class
    /// </summary>
    /// <param name="logflag">
    /// The value of a Hast.Communication.Constants.Catapult.Log flag enum converted into uint.
    /// This can be used to filter out specific types of messages, eg Catapult.Log.Verbose.
    /// </param>
    /// <param name="pchString">The log message. Ends with newline so Write should be used instead of WriteLine.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CatapultLogFunction(uint logflag, string pchString);

    /// <summary>
    /// An interface which describes the functions exposed in FpgaCoreLib.dll.
    /// From the c headers "FPGACoreLib.h" and "FPGAManagementLib.h".
    /// </summary>
    [NativeSymbols(Prefix = "FPGA_")]
    public interface ICatapultLibrary : IDisposable
    {
        #region FPGAManagementLib
        /// <summary>
        /// This function retrieves the total number of shell registers.
        /// </summary>
        Catapult.Status GetNumberShellRegisters(IntPtr fpgaHandle, out uint numShellRegs);
        /// <summary>
        /// This function allows users to read shell registers.  It is both multithreaded- and multiprocesss-safe.
        /// </summary>
        Catapult.Status ReadShellRegister(IntPtr fpgaHandle, uint registerNumber, out uint readValue);
        /// <summary>
        /// This function allows users to write shell registers. It is both multithreaded- and multiprocess-safe.
        /// </summary>
        Catapult.Status WriteShellRegister(IntPtr fpgaHandle, uint registerNumber, uint writeValue);
        /// <summary>
        /// This functions returns whether the loaded FPGA image is a Golden image or not.
        /// </summary>
        Catapult.Status IsGoldenImage(IntPtr fpgaHandle, out bool isGolden, out uint roleID, out uint roleVersion);
        /// <summary>
        /// This function returns the RSU Image ID 
        /// </summary>
        Catapult.Status ReadRsuImageId(IntPtr fpgaHandle, out uint pImageId);
        /// <summary>
        /// This function forces FPGA reconfiguration. It is not multithreaded- or multiprocess-safe.
        /// The act of re-configuration will cause the existing FPGA handles to be closed.
        /// </summary>
        Catapult.Status Reconfig(IntPtr[] fpgaHandles, uint numHandles, bool reconfigToGolden);
        /// <summary>
        /// This function is used to update the flash images on the FPGA. It can either be used to update the golden or app image.
        /// Writing to the golden image requires explicitly passing in FPGA_HANDLE_FLAG_WRITEGOLDEN during handle creation.
        /// WARNING: writing a bad golden image to the FPGA is a career-limiting move and can cause irreparable damage.
        /// <summary>
        /// This function is protected by an internal mutex against other calls to 'FPGA_WriteFlashImage*' and 'FPGA_CaptureFlashImage*'.
        /// The 'timeoutInMs' argument determines how long (in milliseconds) the function waits for acquiring the mutex.  The default wait time is set
        /// according to the worst-case flash write time multiplied by 2 (5 minutes). Setting timeoutInMs to INFINITE removes the timeout.
        /// When a timeout is triggered, this function returns FPGA_STATUS_FLASH_MUTEX_TIMEOUT.
        /// </summary>
        Catapult.Status WriteFlashImageEx(IntPtr fpgaHandle, bool writeToGolden, string RpdFilename, uint timeoutInMs = Catapult.DefaultFlashAccessTimeoutInMilliseconds);
        /// <summary>
        /// This function is preserved for backwards compatibility. Unlike FPGA_WriteFlashImageEx, this function requires an
        /// exclusive handle.
        /// </summary>
        Catapult.Status WriteFlashImage(IntPtr fpgaHandle, bool writeToGolden, string RpdFilename);
        /// <summary>
        /// This function is used to read the flash images from the FPGA to a file. It can either be used to read the golden or app image.
        /// <summary>
        /// This function is protected by an internal mutex against other calls to 'FPGA_WriteFlashImage*' and 'FPGA_CaptureFlashImage*'.
        /// The 'timeoutInMs' argument determines how long the function waits (in milliseconds) for acquiring the mutex.  The default wait time is set
        /// according to the worst-case flash write time multiplied by 2 (5 minutes). Setting timeoutInMs to INFINITE removes the timeout.
        /// When a timeout is triggered, this function returns FPGA_STATUS_FLASH_MUTEX_TIMEOUT.
        /// </summary>
        Catapult.Status CaptureFlashImageEx(IntPtr fpgaHandle, bool isGolden, string RpdFilename, uint timeoutInMs = Catapult.DefaultFlashAccessTimeoutInMilliseconds);
        /// <summary>
        /// This function is preserved for backwards compatibility. Unlike FPGA_CaptureFlashImageEx, this function requires an
        /// exclusive handle.
        /// </summary>
        Catapult.Status CaptureFlashImage(IntPtr fpgaHandle, bool isGolden, string RpdFilename);
        /// <summary>
        /// Disable non-maskable interrupt error reporting.  Needed during FPGA reconfiguration. It is not multithreaded- or multiprocess-safe.
        /// </summary>
        Catapult.Status DisableNMI(IntPtr fpgaHandle);
        /// <summary>
        /// Sanity check: FPGA reconfig often fails simply because the FPGA vendor/device ID changed after reconfig
        /// Will need to use RSU -rootportReload first in order to restore the ability to open an FPGA handle
        /// </summary>
        Catapult.Status ReadVendorDeviceId(IntPtr fpgaHandle, out uint vendorId, out uint deviceId);
        /// <summary>
        /// Disables and re-enables instances of only the port filter driver that are believed to be upstream of the FPGA.
        /// May not target all needed instances, but this should definitely not target network or GPU or other things that
        /// sometimes make FPGA_CycleRootportEnabledNoDriver() crash the machine.
        /// </summary>
        Catapult.Status ReviveRootportNoDriver();
        /// <summary>
        /// Disables and re-enables all instances of the root port filter driver.
        /// Useful if the FPGA device fails to start after reconfig, potentially saves a reboot
        /// </summary>
        Catapult.Status CycleRootportEnabledNoDriver();
        /// <summary>
        /// Disables and re-enables the FPGA in device manager with no driver installed (?).  It is not multithreaded- or multiprocess-safe.
        /// </summary>
        Catapult.Status CycleDeviceEnabledNoDriver();
        /// <summary>
        /// Used in JTAG-based reconfiguration. Call this function before JTAG reconfiguration to disable AER and error reporting. It is not multithreaded- or multiprocess-safe.
        /// </summary>
        Catapult.Status PrepForReconfig(IntPtr[] fpgaHandles, uint numHandles);
        /// <summary>
        /// Used in JTAG-based reconfiguration. Call this function after JTAG reconfiguration to re-enable AER and error reporting. It is not multithreaded- or multiprocess-safe.
        /// </summary>
        Catapult.Status ResetAfterReconfig(IntPtr[] fpgaHandles, uint numHandles);
        /// <summary>
        /// Retrieves FPGA shell statistics.
        /// </summary>
        Catapult.Status ReadStatistic(IntPtr fpgaHandle, uint statType, out uint value);
        /// <summary>
        /// This function enables or disables network bypass mode
        /// Setting 'forceBypass' to TRUE will disable the soft shell network and cause all network traffic to go through the shell bridge.
        /// </summary>
        Catapult.Status SetNetworkBypass(IntPtr fpgaHandle, bool forceBypass);
        /// <summary>
        /// Trigger soft reset.
        /// </summary>
        Catapult.Status TriggerSoftReset(IntPtr fpgaHandle, uint resetType);
        /// <summary>
        /// Attempt to set the slot size and number of slots. Will fail if requested settings are invalid or cannot be attained.
        /// Bytes per slot must be a multiple of 4096. Number of slots cannot be larger than that supported by hardware.
        /// </summary>
        Catapult.Status SetSlotSize(IntPtr[] fpgaHandles, uint numHandles, uint bytesPerSlot, uint numSlots);
        #endregion

        #region FPGACoreLib
        // Most functions in the API below are MULTIPROCESS- and MULTI-THREADED SAFE, with the following exceptions:
        //   - FPGA_CreateHandle is multiprocess-safe, but NOT multithreaded-safe.  It should only be called once 
        //     per process, per end-point.
        //   - FPGA_CloseHandle has the same constraints as FPGA_CreateHandle.
        // 
        // All functions return an FPGA_STATUS code that should be checked for FPGA_STATUS_SUCCESS by the user.

        /// <summary>
        /// The FPGA_IsDevicePresent can be used to determine if a compatible Catapult device is installed in the system.
        /// During service rollout, there is a time window in which the service cannot be sure that the FPGA device installer
        /// has had a chance to run. During that time window, the service should wait and retry to open the FPGA handle
        /// if and only if a catapult device is present in the system.
        /// </summary>
        /// <remarks>
        /// pchVerManifestFile:   Defines compatible values for version registers. Leave as NULL to use default "DefaultVersionManifest.ini".
        ///                       Unless an absolute path is specified, INI files are searched relative to the DLL path.
        /// The optional last argument allows the user to specify a logging callback function.
        /// The user-defined function must accept a 'loglevel' argument, which classifies the log entry (e.g., verbose, informational, debug, etc.).
        /// and an ANSI string, which contains a single log entry.
        /// The FPGA_LOG_* flags near the top of this header file define all the possible (and non-mutually-exclusive) severity levels.
        ///
        /// The default version manifest file passed in must contain a section of the following form:
        /// 
        /// [PnP]
        /// ; [PnP]\CompatibleDeviceIds
        /// ; -Comma - separated list of device ID strings.
        /// ; -At least one of these devices must be detected for a driver installation to proceed.
        /// ;   If none are found, the service exits without taking any installation action.
        /// CompatibleDeviceIds = PCI\VEN_1414&DEV_B100, PCI\VEN_1414&DEV_B101, PCI\VEN_1172&DEV_E001
        ///
        /// Possible result codes: 
        ///  FPGA_STATUS_SUCCESS                 - Device present
        ///  FPGA_STATUS_HARDWARE_NOT_PRESENT    - Device not present
        /// </remarks>
        Catapult.Status IsDevicePresent(string pchVerManifestFile, [MarshalAs(UnmanagedType.FunctionPtr)]CatapultLogFunction logFunc);
        /// <summary>
        /// The FPGA_CreateHandle function instantiates and returns a handle to an FPGA device.
        /// Since multiple PCIe endpoints can exist on an FPGA, the user can specify a number 
        /// when referring to a specific PCIe endpoint.  Most typical usage cases
        /// will never require more than 1 endpoint, so the value should be left as 0 by default.
        /// </summary>
        /// <remarks>
        /// An additional 'flags' argument permits additional options that are reserved for future use.
        ///
        /// The next two arguments define the files that are used to perform version checking of the FPGA bitstream at run-time.
        ///   pchShellVerDefnsFile: Defines which version registers to verify. Leave as NULL to use default "VersionDefinitions.ini".
        ///   pchVerManifestFile:   Defines compatible values for version registers. Leave as NULL to use default "DefaultVersionManifest.ini".
        ///   Unless an absolute path is specified, INI files are searched relative to the DLL path.
        ///
        /// The optional last argument allows the user to specify a logging callback function.
        /// The user-defined function must accept a 'loglevel' argument, which classifies the log entry (e.g., verbose, informational, debug, etc.).
        /// and an ANSI string, which contains a single log entry.
        /// The FPGA_LOG_* flags near the top of this header file define all the possible (and non-mutually-exclusive) severity levels.
        ///
        /// If the device is disable and the caller has administrative privileges, FPGA_CreateHandle will attempt to enable the device and then open a handle
        /// </remarks>
        Catapult.Status CreateHandle(out IntPtr fpgaHandle, uint endpointNumber, uint flags, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pchVerDefnsFile, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pchVersionManifestFile, [MarshalAs(UnmanagedType.FunctionPtr)]CatapultLogFunction logFunc = null);
        /// <summary>
        /// This function allows the library user to query the version of both software and hardware componetns.
        /// All queryable version keys are defined in the 'pchVerDefnsFile' argument of FPGA_CreateHandle. 
        /// By default, the definitions are listed in VersionDefinitions.ini.
        /// </summary>
        Catapult.Status GetVersion(IntPtr fpgaHandle, string pchVerKey, out uint pVersion);
        /// <summary>
        /// This function should be called to release and close an FPGA handle.
        /// </summary>
        Catapult.Status CloseHandle(IntPtr fpgaHandle);
        // /// <summary>
        // /// This function retrieves the total number of available PCIe endpoints on an FPGA.
        // /// </summary>
        // Catapult.Status GetNumberEndpoints(IntPtr fpgaHandle, out uint numEndpoints);
        /// <summary>
        /// This function retrieves the total number of PCIe buffers available for host-to-FPGA communication.
        /// </summary>
        Catapult.Status GetNumberBuffers(IntPtr fpgaHandle, out uint numBuffers);
        /// <summary>
        /// This function retrieves the total number of bytes available to use, per FPGA PCIe buffer.
        /// </summary>
        Catapult.Status GetBufferSize(IntPtr fpgaHandle, out uint bufferBytes);
        /// <summary>
        /// Returns the last error occurred.
        /// </summary>
        Catapult.Status GetLastError();
        /// <summary>
        /// Provides error string for the last error occurred.
        /// </summary>
        Catapult.Status GetLastErrorText([MarshalAs(UnmanagedType.LPStr)]StringBuilder pErrorTextBuf, int cbBufSize);
        /// <summary>
        /// This function retrieves a user-mapped pointer to a pinned region of kernel memory that is typically
        /// written by the host system and read by the FPGA. The available buffer capacity is finite and should
        /// be queried using "FPGA_GetBufferSize". The total number of buffers available can be queried
        /// using "FPGA_GetNumberBuffers". 
        /// </summary>
        Catapult.Status GetInputBufferPointer(IntPtr fpgaHandle, uint whichBuffer, out IntPtr inputBufferPtr);
        /// <summary>
        /// This function retrieves a user-mapped pointer to a pinned region of kernel memory that is typically written
        /// by the FPGA, and read by the host system. The available buffer capacity is finite and should be
        /// queried using "FPGA_GetBufferSize". 
        /// </summary>
        Catapult.Status GetOutputBufferPointer(IntPtr fpgaHandle, uint whichBuffer, out IntPtr outputBufferPtr);
        /// <summary>
        /// This function retrieves a user-mapped pointer to a pinned region of kernel memory that is typically written
        /// by the FPGA, and read by the host system. The result buffer includes additional metadata associated
        /// with a message received on the output buffer. Currently, the result buffer will store the total number of
        /// bytes received from the FPGA. 
        /// </summary>
        Catapult.Status GetResultBufferPointer(IntPtr fpgaHandle, uint whichBuffer, out IntPtr resultBufferPtr);
        /// <summary>
        /// This function allows users to read soft shell registers. This function is thread- and multiprocess-safe; however
        /// the current implementation of this API uses multiple non-atomic PIOs to the shell protected by a software named global mutex.
        /// </summary>
        Catapult.Status ReadSoftRegister(IntPtr fpgaHandle, uint address, out ulong readValue);
        /// <summary>
        /// This function allows users to write soft shell registers. The same conditions described in 'FPGA_ReadSoftRegister'
        /// apply here as well.
        /// </summary>
        Catapult.Status WriteSoftRegister(IntPtr fpgaHandle, uint address, ulong writeValue);
        /// <summary>
        /// This function indicates whether an input buffer is full and unavailable for writing by the host.
        /// </summary>
        Catapult.Status GetInputBufferFull(IntPtr fpgaHandle, uint whichBuffer, out bool isFull);
        /// <summary>
        /// This function indicates whether an output buffer was written by the FPGA, and available for reading by the host.
        /// </summary>
        Catapult.Status GetOutputBufferDone(IntPtr fpgaHandle, uint whichBuffer, out bool isDone);
        /// <summary>
        /// This function initiates a send operation that transfers the contents of a specified input buffer to the FPGA.
        /// This function is non-blocking and returns immediately to the user. The user can specify whether an interrupt 
        /// should be generated when a message is written to the corresponding output buffer by the FPGA.  
        /// Note: most applications should enable the use of interrupts, unless polling is absolutely necessary.  
        /// An interrupt-enabled call to "FPGA_SendInputBuffer" must be paired with an interrupt-enabled call 
        /// to "FPGA_WaitOutputBuffer". 
        /// </summary>
        /// <remarks>
        /// Users are responsible for verifying that the input buffer is available for use prior to calling this function, 
        /// e.g., by checking the return value of "FPGA_GetInputBufferFull". 
        /// </remarks>
        Catapult.Status SendInputBuffer(IntPtr fpgaHandle, uint whichBuffer, uint sizeBytes, bool useInterrupt = true);
        /// <summary>
        /// This function blocks the calling thread until a message is received from the FPGA for a given output buffer. 
        /// The user must provide storage for the FPGA-generated PCIe header, and must also specify whether an interrupt is
        /// expected or polling should be used.  If an interrupt is expected, a previous interrupt-enabled call to
        /// FPGA_SendInputBuffer must have been made to the same corresponding input buffer number. 
        /// The timeout parameter, when overriden from the default, will cause the calling thread to unblock
        /// after a specified duration (in seconds). Setting the timeout to zero will result in an INFINITE wait.
        /// </summary>
        /// <remarks>
        /// On a timeout, a FPGA_STATUS_WAIT_TIMEOUT is returned. Otherwise,
        /// the function returns FPGA_STATUS_SUCCESS. 
        /// 
        /// pBytesReceived can receive a NULL argument, if the user wishes to discard the total number of bytes received.
        /// On a successful return, the function returns the total number of bytes received from the FPGA. 
        /// </remarks> 
        Catapult.Status WaitOutputBuffer(IntPtr fpgaHandle, uint whichBuffer, out uint pBytesReceived, bool useInterrupt = true, double timeoutInSeconds = Catapult.WaitOutputBufferTimeoutDefaultSeconds);
        /// <summary>
        /// This function allows a user to discard a particular output buffer after its contents have already
        /// been consumed and are no longer needed. The user should typically call "FPGA_DiscardOutputBuffer"
        /// after waiting on an output buffer using "FPGA_WaitOutputBuffer".
        /// </summary>
        /// <remarks>
        /// It is harmless to call this function even if no data is expected from the FPGA. 
        /// </remarks>
        Catapult.Status DiscardOutputBuffer(IntPtr fpgaHandle, uint whichBuffer);
        #endregion
    }
}
