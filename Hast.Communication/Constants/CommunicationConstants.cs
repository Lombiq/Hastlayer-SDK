using System;
using System.IO.Ports;

namespace Hast.Communication.Constants
{
    public static class CommunicationConstants
    {
        public static class Catapult
        {
            public const string ChannelName = nameof(Catapult);

            public const uint RegisterMaskPcieEnabled = (1 << 6);

            #region unique constants from header files
            public const int DefaultFlashAccessTimeoutInMilliseconds = (5 * 60 * 1000);
            public const string ErrorLabels = "hr:min:sec:ms,cycles,pid,tid,filename,line,errmsg,";
            public const string DefaultVersionManifestFile = "FPGADefaultVersionManifest.ini";
            public const string VersionDefinitionsFile = "FPGAVersionDefinitions.ini";
            public static readonly Version LibraryVersion = new Version(3, 40);
            public const int SoftResetRole = 14;
            public const double WaitOutputBufferTimeoutDefaultSeconds = 10.0;
            public const int PcieHipNumber = 0;
            #endregion

            #region grouped constants from header files
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
                UnknownError = 10000,
            }

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

            public class HandleFlag
            {
                public const uint None = 0x0000000u;
                public const uint Verbose = 0x0000001u;
                public const uint Diagnostics = 0x0000002u;
                public const uint Exclusive = 0x0001000u; // Must be passed to use functions in FPGAManagementLib.h (e.g., flash write, reconfig).
                public const uint Writegolden = 0x0002000u; // Must be passed in when updating the golden image.
            }

            public enum Stat
            {
                CyclesLower = 8,
                CyclesUpper = 9,
                CrcErrors = 10,
                Dram0SingleErrors = 11,
                Dram0DoubleErrors = 12,
                Dram0UncorrectedErrors = 13,
            }

            public enum ConfigDram
            {
                Channel0 = 600,
                Channel1 = 700,
                Interleaved = 800,
            }
            #endregion
        }

        public static class Ethernet
        {
            public const string ChannelName = "Ethernet";


            public static class Ports
            {
                public const int WhoIsAvailableRequest = 34050;
                public const int WhoIsAvailableResponse = 33000;
            }


            public static class Signals
            {
                public const char Busy = 'b';
                public const char Ready = 'r';
            }
        }

        public static class Serial
        {
            public const int DefaultBaudRate = 9600;
            public const Parity DefaultParity = Parity.None;
            public const StopBits DefaultStopBits = StopBits.One;
            public const int DefaultWriteTimeoutMilliseconds = 10000;
            public const string ChannelName = "Serial";


            public static class Signals
            {
                public const char Ping = 'p';
                public const char Ready = 'r';
            }


            public enum CommunicationState
            {
                WaitForFirstResponse,
                ReceivingExecutionInformation,
                ReceivingOutputByteCount,
                ReceivingOuput,
                Finished
            }
        }
    }
}
