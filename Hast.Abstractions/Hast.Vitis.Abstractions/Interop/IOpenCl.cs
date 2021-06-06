using System;
using AdvancedDLSupport;
using Hast.Vitis.Abstractions.Interop.Delegates.OpenCl;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Interop
{
    [NativeSymbols(Prefix = "cl")]
    public interface IOpenCl : IDisposable
    {
        Result GetPlatformIDs(uint numberOfEntries, IntPtr[] platforms, out uint numberOfPlatforms);

        Result GetPlatformInfo(IntPtr platform, PlatformInformation parameterName, UIntPtr parameterValueSize, byte[] parameterValue, out UIntPtr parameterValueSizeReturned);

        Result GetDeviceIDs(IntPtr platform, DeviceTypes deviceTypes, uint numberOfEntries, IntPtr[] devices, out uint numberOfDevicesReturned);

        IntPtr CreateContext(IntPtr properties, uint numberOfDevices, IntPtr[] devices, IntPtr notificationCallback, IntPtr userData, out Result errorCode);

        Result ReleaseContext(IntPtr context);

        IntPtr CreateCommandQueue(IntPtr context, IntPtr device, CommandQueueProperties propertieses, out Result errorCode);

        Result ReleaseCommandQueue(IntPtr commandQueue);

        IntPtr CreateProgramWithBinary(IntPtr context, int numberOfDevices, IntPtr[] deviceList, int[] lengths, IntPtr[] binaries, Result[] binaryStatus, out Result errorCode);

        Result BuildProgram(IntPtr program, int numberOfDevices, IntPtr[] deviceList, string options, BuildCallback notify, IntPtr userData);

        IntPtr CreateKernel(IntPtr program, string kernelName, out Result errorCode);

        Result ReleaseKernel(IntPtr kernel);

        IntPtr CreateBuffer(IntPtr context, MemoryFlags flagses, int size, IntPtr hostPointer, out Result errorCode);

        Result ReleaseMemObject(IntPtr buffer);

        Result SetKernelArg(IntPtr kernel, uint argumentIndex, UIntPtr argumentSize, IntPtr argumentValue);

        Result EnqueueMigrateMemObjects(
            IntPtr commandQueue,
            uint numberOfMemoryObjects,
            IntPtr[] memoryObjects,
            MemoryMigrationFlags memoryMigrationFlagses,
            uint numberOfEventsInWaitList,
            IntPtr[] eventWaitList,
            out IntPtr waitEvent);

        Result EnqueueTask(IntPtr commandQueue, IntPtr kernel, uint numberOfEventsInWaitList, IntPtr[] eventWaitList, out IntPtr waitEvent);

        Result Finish(IntPtr commandQueue);
    }
}
