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
        /// <summary>
        /// Gets the number of devices based on the count of handlers received from the API.
        /// </summary>
        int DeviceCount { get; }

        /// <summary>
        /// Creates a kernel from the already compiled <paramref name="binary"/> data and stores it with the
        /// <paramref name="kernelName"/> identifier.
        /// </summary>
        void CreateBinaryKernel(byte[] binary, string kernelName);

        /// <summary>
        /// Creates a new command queue for the device identified by the numeric index <paramref name="deviceIndex"/>.
        /// </summary>
        void CreateCommandQueue(
            int deviceIndex,
            CommandQueueProperties properties = CommandQueueProperties.ProfilingEnable);

        /// <summary>
        /// Starts execution of the kernel identified by <paramref name="kernelName"/> on the device identified by the
        /// numeric index <paramref name="deviceIndex"/>.
        /// </summary>
        /// <param name="deviceIndex">The numeric ID of the target device.</param>
        /// <param name="kernelName">The name passed to <see cref="CreateBinaryKernel"/>.</param>
        /// <param name="buffers">
        /// The handler(s) received from <see cref="CreateBuffer"/>. It is necessary to call
        /// <see cref="SetKernelArgumentWithNewBuffer"/> on each item before calling this method.
        /// </param>
        /// <param name="copyBack">
        /// If <see langword="true" />, the <paramref name="buffers"/> are overwritten by the kernel.
        /// </param>
        void LaunchKernel(int deviceIndex, string kernelName, IntPtr[] buffers, bool copyBack = true);

        /// <summary>
        /// Asynchronously waits for the device identified by the numeric index <paramref name="deviceIndex"/> to finish
        /// any scheduled executions.
        /// </summary>
        Task AwaitDeviceAsync(int deviceIndex);

        /// <summary>
        /// Registers the <paramref name="buffer"/> as an argument for the kernel. If the buffer doesn't yet exist, it
        /// gets created and initialized with the provided <paramref name="data"/>.
        /// </summary>
        /// <param name="kernelName">The name passed to <see cref="CreateBinaryKernel"/>.</param>
        /// <param name="index">The zero-based index of the argument position.</param>
        /// <param name="data">
        /// The native pointer to data in memory. It can be acquired for example from <see cref="Memory{T}.Pin"/>.
        /// </param>
        /// <param name="length">The amount of bytes in the <paramref name="data"/>.</param>
        /// <param name="buffer">The handle of the buffer from <see cref="CreateBuffer"/> or <see cref="IntPtr.Zero"/>.</param>
        /// <returns>
        /// If <paramref name="buffer"/> is not <see cref="IntPtr.Zero"/> then its value. Otherwise the handler for the
        /// freshly created buffer.
        /// </returns>
        IntPtr SetKernelArgumentWithNewBuffer(
            string kernelName, int index, MemoryHandle data, int length, IntPtr buffer = default);

        /// <summary>
        /// Creates a new buffer and fills it with data.
        /// </summary>
        /// <param name="hostPointer">The pointer to the data in the host memory.</param>
        /// <param name="hostBytes">The length of the data in bytes.</param>
        /// <param name="memoryFlags">The flags to be passed to the native buffer creator method.</param>
        /// <returns>The handler for the newly created buffer.</returns>
        IntPtr CreateBuffer(IntPtr hostPointer, int hostBytes, MemoryFlags memoryFlags);

        /// <summary>
        /// Prepares the device querying logic with the provided <paramref name="configuration"/>. This must be called
        /// before anything else.
        /// </summary>
        void PrepareDevices(IOpenClConfiguration configuration);
    }
}
