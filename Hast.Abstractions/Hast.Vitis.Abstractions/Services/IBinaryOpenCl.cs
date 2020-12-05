using Hast.Common.Interfaces;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using System;
using System.Buffers;
using System.Threading.Tasks;
using Hast.Vitis.Abstractions.Models;

namespace Hast.Vitis.Abstractions.Services
{
    /// <summary>
    /// A managed wrapper around OpenCL for using precompiled binary kernels. It abstracts resource management and hides
    /// most pointer usage, within practicality.
    /// </summary>
    /// <remarks>
    /// <para>It is of singleton lifecycle because it handles hardware and binary resources which are not expected to change
    /// while the application is running. This way subsequent runs don't have to suffer the same initialization costs.</para>
    /// </remarks>
    public interface IBinaryOpenCl : ISingletonDependency, IDisposable
    {
        int DeviceCount { get; }

        void CreateBinaryKernel(byte[] binary, string kernelName);

        void CreateCommandQueue(
            int deviceIndex,
            CommandQueueProperty properties = CommandQueueProperty.ProfilingEnable);

        void LaunchKernel(int deviceIndex, string kernelName, IntPtr[] buffers, bool copyBack = true);
        Task AwaitDevice(int deviceIndex);

        IntPtr SetKernelArgumentWithNewBuffer(
            string kernelName, int index, MemoryHandle data, int length, IntPtr buffer = default);

        IntPtr CreateBuffer(IntPtr hostPointer, int hostBytes, MemoryFlag memoryFlags);
        void PrepareDevices(IOpenClConfiguration configuration);
    }
}
