using System;
using System.Diagnostics.CodeAnalysis;

namespace Hast.Catapult.Abstractions
{
    public static class Constants
    {
        public const string ChannelName = "Catapult";

        public const string DefaultLibraryPath = "FpgaCoreLib";

        /// <summary>
        /// The binary mask for the PCIe enable flag in the shell register.
        /// </summary>
        public const uint RegisterMaskPcieEnabled = 1 << 6;

        /// <summary>
        /// The minimum length of a message sent to the input buffer in bytes.
        /// </summary>
        public const int BufferMessageSizeMinByte = 64;

        /// <summary>
        /// The buffer must be multiples of this size.
        /// </summary>
        public const int BufferChunkBytes = 64;

        public static class SoftRegisters
        {
            public const int AllowedSlots = 2;
            public const int BufferPayloadSize = 3;
        }

        public static class ConfigKeys
        {
            public const string LibraryPath = nameof(LibraryPath);
            public const string VersionDefinitionsFile = nameof(VersionDefinitionsFile);
            public const string VersionManifestFile = nameof(VersionManifestFile);
        }

        #region Unique constants from header files
        public const int DefaultFlashAccessTimeoutInMilliseconds = 5 * 60 * 1000;
        public const string ErrorLabels = "hr:min:sec:ms,cycles,pid,tid,filename,line,errmsg,";
        public const string DefaultVersionManifestFile = "FPGADefaultVersionManifest.ini";
        public const string VersionDefinitionsFile = "FPGAVersionDefinitions.ini";
        public static readonly Version LibraryVersion = new Version(3, 40);
        public const int SoftResetRole = 14;
        public const double WaitOutputBufferTimeoutDefaultSeconds = 10.0;
        public const int PcieHipNumber = 0;
        public const int MaxBufferSizeBytes = 65536;
        #endregion

        #region Grouped constants from header files
        /// <summary>
        /// The return value of the functions in the native Catapult FPGA library.
        /// It indicates the success or error state of the function call.
        /// </summary>
        public enum Status
        {
            Success = 0,
            NullPointerGiven = 1,
            MemoryNotAllocated = 2,
            IllegalEndpointNumber = 3,
            ErrorOpeningDevice = 4,
            ErrorNullDeviceHandle = 5,
            FailedToCreateMutex = 6,
            FailedToAllocateIsr = 7,
            FailedToCreateEvent = 8,
            ErrorEnablingInterrupts = 9,
            InvalidBufferSize = 10,
            InvalidBufferCount = 11,
            InvalidRegisterCount = 12,
            InsufficientBuffersAllocated = 13,
            HeapFreeFailed = 14,
            IllegalSize = 15,
            BadSizeReturned = 16,
            InvalidReturnedSlot = 17,
            IllegalBufferNumber = 18,
            IllegalRegisterNumber = 19,
            IncompatibleHardware = 20,
            VersionManifestError = 21,
            UnableToAccessSysdir = 22,
            UnableToReadDriverVersion = 23,
            UndefinedVersionKey = 24,
            MemcpyError = 25,
            IncompatibleSoftware = 26,
            IncompatibleHeader = 27,
            Unimplemented = 28,
            LockFailure = 29,
            DmaBufferError = 30,
            WaitTimeout = 31,
            ReconfigFailed = 32,
            CloseError = 33,
            DeviceCycle = 34,
            PowerError = 35,
            WaitFailed = 36,
            ReconfigToGoldenFailed = 37,
            ReconfigToApplicationFailed = 38,
            MemoryLeakDetected = 39,
            ErrorOpeningFile = 40,
            FlashError = 41,
            ErrorOpeningRootport = 42,
            ErrorNullRootportHandle = 43,
            ErrorClosingRootport = 44,
            ErrorGettingRootportCount = 45,
            InvalidRootportOffset = 46,
            InvalidRootportInstance = 47,
            ErrorReadingRootport = 48,
            ErrorWritingRootport = 49,
            RootportNoLinkCtrlOffset = 50,
            RootportInstanceNotFound = 51,
            FailedGetInitState = 52,
            FailedSetInitState = 53,
            FailedParentWriteCfg = 54,
            FailedParentReadCfg = 55,
            FailedFpgaWriteCfg = 56,
            FailedFpgaReadCfg = 57,
            ExpectedCapabilityNotFound = 58,
            FailedToRetrainLink = 59,
            MutexError = 60,
            BadHandleCount = 61,
            FailedWrite = 62,
            BadArgument = 63,
            InvalidSlotConfig = 64,
            RequiresElevatedPrivileges = 65,
            DeviceDriverFailedDisable = 66,
            DeviceDriverFailedEnable = 67,
            CannotUseWithDiagnostics = 68,
            HandleInsufficientPermission = 69,
            CannotOpenMultipleHandles = 70,
            CannotCloseMultipleHandles = 71,
            RootportHandleLocked = 72,
            CannotLockRootportHandle = 73,
            CannotLockOnNonPrimaryHip = 74,
            RequiresExclusiveHandle = 75,
            HardwareNotPresent = 76,
            UnsupportedFlashSlot = 77,
            UnrecognizedConfigurationFlash = 78,
            FlashMutexTimeout = 79,
            EnumerationMoreDevices = 80,
            EnumerationNoList = 81,
            UnknownError = 10000
        }

        /// <summary>
        /// The type flags used in the logger function.
        /// </summary>
        [Flags]
        public enum Log
        {
            None = 0x00,
            Info = 0x01,
            Debug = 0x03,
            Verbose = 0x04,
            Error = 0x08,
            Fatal = 0x10,
            Warn = 0x20,
        }

        public static class HandleFlag
        {
            public const uint None = 0x0000000u;
            public const uint Verbose = 0x0000001u;
            public const uint Diagnostics = 0x0000002u;
            /// <summary>
            /// Must be passed to use functions in FPGAManagementLib.h (e.g., flash write, reconfig).
            /// </summary>
            public const uint Exclusive = 0x0001000u;
            /// <summary>
            /// Must be passed in when updating the golden image.
            /// </summary>
            public const uint WriteGolden = 0x0002000u;
        }

        [SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Not applicable.")]
        public enum Stat
        {
            CyclesLower = 8,
            CyclesUpper = 9,
            CrcErrors = 10,
            Dram0SingleErrors = 11,
            Dram0DoubleErrors = 12,
            Dram0UncorrectedErrors = 13,
        }

        public static class ConfigDram
        {
            public const uint Channel0 = 600;
            public const uint Channel1 = 700;
            public const uint Interleaved = 800;
        }
        #endregion

        #region Header sizes
        /// <summary>
        /// Sizes of the input headers in bytes.
        /// </summary>
        public static class InputHeaderSizes
        {
            /// <summary>
            /// The member ID in Hast_IP.
            /// </summary>
            public const int MemberId = sizeof(int);

            /// <summary>
            /// Total length of the sent data in bytes without the headers.
            /// </summary>
            public const int PayloadLengthCells = sizeof(int);

            /// <summary>
            /// Zero-based index of the current data slice (at least zero and less than sliceCount). If the data is
            /// more than what would fit into a single slot then the header for the second slot will contain the
            /// SliceIndex 1, for the third 2, and for the last one <see cref="SliceCount"/> - 1.
            /// </summary>
            public const int SliceIndex = sizeof(int);

            /// <summary>
            /// The number of slices the data is transmitted in. If the data has fewer bytes than BufferPayloadSize
            /// then it's always 1. Otherwise it's PayloadLengthCells / BufferPayloadSize rounded up. If there are
            /// more input slices than slots on the hardware, then the response will behave as if the input sliceCount
            /// was only 1.
            /// </summary>
            public const int SliceCount = sizeof(int);

            /// <summary>
            /// The length of the input (request) header in bytes.
            /// </summary>
            public const int Total = MemberId + PayloadLengthCells + SliceIndex + SliceCount;
        }

        /// <summary>
        /// Sizes of the output headers in bytes.
        /// </summary>
        public static class OutputHeaderSizes
        {
            /// <summary>
            /// The number of clock cycles between Hast_IP's Start and Finished signals.
            /// </summary>
            public const int HardwareExecutionTime = sizeof(long);

            /// <summary>
            /// Total length of the received data in cells without the headers.
            /// </summary>
            public const int PayloadLengthCells = sizeof(int);

            /// <summary>
            /// Zero-based index of the current data slice (at least zero and less than sliceCount). If the data is
            /// more than what would fit into a single slot then the header for the second slot will contain the
            /// SliceIndex 1, for the third 2, and for the last one <see cref="SliceCount"/> - 1.
            /// </summary>
            public const int SliceIndex = sizeof(int);

            /// <summary>
            /// The length of the output (response) header in bytes.
            /// </summary>
            public const int Total = HardwareExecutionTime + PayloadLengthCells + SliceIndex;
        }
        #endregion
    }
}
