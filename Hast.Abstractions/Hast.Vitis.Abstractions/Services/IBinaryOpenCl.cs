using Hast.Common.Interfaces;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    public interface IBinaryOpenCl : IDependency, IDisposable
    {
        int DeviceCount { get; }

        void CreateBinaryKernel(byte[] binary, string kernelName);

        void CreateCommandQueue(int deviceIndex,
            CommandQueueProperty properties = CommandQueueProperty.ProfilingEnable);

        void LaunchKernel(int deviceIndex, string kernelName, IntPtr[] buffers, bool copyBack = true);
        Task AwaitDevice(int deviceIndex);

        IntPtr SetKernelArgumentWithNewBuffer(
            string kernelName, int index, MemoryHandle data, int length, IntPtr buffer = default);

        IntPtr CreateBuffer(IntPtr hostPointer, int hostBytes, MemoryFlag memoryFlags);
    }
}
