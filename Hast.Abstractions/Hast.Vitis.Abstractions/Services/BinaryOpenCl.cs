using AdvancedDLSupport;
using Hast.Common.Interfaces;
using Hast.Vitis.Abstractions.Interop;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Vitis.Abstractions.Services
{
    [DependencyInitializer(nameof(InitializeService))]
    public sealed class BinaryOpenCl : IBinaryOpenCl
    {
        #region Fields and properties

        public const MemoryFlags DefaultMemoryFlags = MemoryFlags.UseHostPointer | MemoryFlags.ReadWrite;
        private static readonly UIntPtr _intPtrSize = new((uint)Marshal.SizeOf(IntPtr.Zero));

        private readonly IOpenCl _cl;
        private readonly ILogger _logger;

        private readonly Lazy<IntPtr> _context;

        private readonly Dictionary<int, IntPtr> _queues = new();
        private readonly Dictionary<string, IntPtr> _kernels = new();
        private readonly Dictionary<string, List<IntPtr>> _kernelBuffers = new();

        private Lazy<IntPtr[]> _devicesLazy;

        private IntPtr[] Devices => _devicesLazy.Value;
        public int DeviceCount => Devices.Length;

        #endregion

        #region Constructors

        public BinaryOpenCl(
            IOpenCl cl,
            ILogger<BinaryOpenCl> logger)
        {
            _cl = cl;
            _logger = logger;

            _context = new Lazy<IntPtr>(() =>
            {
                if (!Devices.Any()) return IntPtr.Zero;
                var context = _cl.CreateContext(
                    IntPtr.Zero,
                    (uint)Devices.Length,
                    Devices.ToArray(),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out var result);
                VerifyResult(result);
                return context;
            });
        }

        ~BinaryOpenCl() => Dispose();

        #endregion

        #region Methods

        public void PrepareDevices(IOpenClConfiguration configuration) =>
            _devicesLazy ??= new Lazy<IntPtr[]>(() =>
                GetDeviceHandlesOfVendor(configuration.VendorName, configuration.DeviceType).ToArray());

        public void CreateCommandQueue(
            int deviceIndex,
            CommandQueueProperties properties = CommandQueueProperties.ProfilingEnable)
        {
            if (_queues.ContainsKey(deviceIndex)) return;

            var queue = _cl.CreateCommandQueue(_context.Value, Devices[deviceIndex], properties, out var result);
            VerifyResult(result);
            _queues[deviceIndex] = queue;
        }

        public void CreateBinaryKernel(byte[] binary, string kernelName)
        {
            if (_kernels.ContainsKey(kernelName)) return;

            unsafe
            {
                fixed (byte* pointer = binary)
                {
                    var program = CreateProgramWithBinary((IntPtr)pointer, binary.Length);

                    var kernel = _cl.CreateKernel(program, kernelName, out var result);
                    VerifyResult(result);
                    _kernels[kernelName] = kernel;
                    _kernelBuffers[kernelName] = new List<IntPtr>();
                }
            }
        }

        public IntPtr CreateBuffer(IntPtr hostPointer, int hostBytes, MemoryFlags memoryFlags)
        {
            var buffer = _cl.CreateBuffer(_context.Value, memoryFlags, hostBytes, hostPointer, out var result);
            VerifyResult(result);
            return buffer;
        }

        public void LaunchKernel(int deviceIndex, string kernelName, IntPtr[] buffers, bool copyBack = true)
        {
            var queue = GetQueue(deviceIndex);
            var kernel = GetKernel(kernelName);

            var waitEvent = IntPtr.Zero;
            waitEvent = EnqueueMemoryMigration(queue, buffers, false, waitEvent);
            waitEvent = EnqueueTask(queue, kernel, waitEvent);
            if (copyBack) EnqueueMemoryMigration(queue, buffers, true, waitEvent);
        }

        public async Task AwaitDeviceAsync(int deviceIndex)
        {
            var queue = GetQueue(deviceIndex);
            VerifyResult(await Task.Run(() => _cl.Finish(queue)));
        }

        public unsafe IntPtr SetKernelArgumentWithNewBuffer(
            string kernelName,
            int index,
            MemoryHandle data,
            int length,
            IntPtr buffer = default)
        {
            if (buffer == IntPtr.Zero) buffer = CreateBuffer((IntPtr)data.Pointer, length, DefaultMemoryFlags);

            SetKernelArgument(GetKernel(kernelName), index, buffer);
            GetKernelBuffers(kernelName).Add(buffer);
            return buffer;
        }

        [SuppressMessage(
            "Style",
            "IDE0010:Add missing cases",
            Justification = "There are lots of cases, but we only want to treat the special ones.")]
        private static void VerifyResult(Result status)
        {
            switch (status)
            {
                case Result.Success:
                    return;
                case Result.PlatformNotFoundKhr:
                    throw new InvalidOperationException(
                        "No platforms were found. This usually means that the compute device is not powered or " +
                        "connected. If you are cross-compiling for a different machine and the compilation has " +
                        "finished you can disregard this error. (error: CL_PLATFORM_NOT_FOUND_KHR)");
                default:
                    var errorSymbol = "CL_" + status.ToString().ToSnakeCase().ToUpper(CultureInfo.InvariantCulture);
                    throw new InvalidOperationException(
                        $"OpenCL error with status '{status}'. You may find more information by searching for " +
                        $"\"opencl {status} OR {errorSymbol}\" on the web.");
            }
        }

        private static AggregateException VerifyResults(
            IEnumerable<Result> results,
            Func<Result, int, Exception> mapper)
        {
            var errors = results
                .Select((x, i) => (Result: x, Index: i))
                .Where(x => x.Result != Result.Success)
                .Select(x => mapper(x.Result, x.Index))
                .ToList();
            return errors.Count > 0 ? new AggregateException(errors) : null;
        }

        private IEnumerable<IntPtr> GetPlatformHandles(string vendorName = null)
        {
            VerifyResult(_cl.GetPlatformIDs(0, null, out uint count));

            var platforms = new IntPtr[count];
            VerifyResult(_cl.GetPlatformIDs(count, platforms, out _));

            return string.IsNullOrEmpty(vendorName) ?
                platforms :
                platforms.Where(platform =>
                {
                    VerifyResult(_cl.GetPlatformInfo(
                        platform,
                        PlatformInformation.Name,
                        UIntPtr.Zero,
                        null,
                        out var size));
                    var output = new byte[size.ToUInt32()];
                    VerifyResult(_cl.GetPlatformInfo(platform, PlatformInformation.Name, size, output, out _));

                    return vendorName == Encoding.UTF8.GetString(output, 0, output.Length).TrimEnd('\0');
                });
        }

        private IntPtr[] GetDeviceHandles(IntPtr platform, DeviceTypes deviceType)
        {
            VerifyResult(_cl.GetDeviceIDs(platform, deviceType, 0, null, out uint numberOfAvailableDevices));

            var devicePointers = new IntPtr[numberOfAvailableDevices];
            VerifyResult(_cl.GetDeviceIDs(platform, deviceType, numberOfAvailableDevices, devicePointers, out _));

            return devicePointers;
        }

        private IEnumerable<IntPtr> GetDeviceHandlesOfVendor(string vendorName, DeviceTypes deviceType)
        {
            var foundDevice = false;

            foreach (var platform in GetPlatformHandles(vendorName))
            {
                foreach (var device in GetDeviceHandles(platform, deviceType))
                {
                    foundDevice = true;
                    yield return device;
                }
            }

            if (!foundDevice)
            {
                _logger?.LogWarning(
                    "Failed to find \"{0}\" platform that has \"{1}\" type devices.",
                    vendorName,
                    deviceType);
            }
        }

        private IntPtr CreateProgramWithBinary(IntPtr binary, int binaryLength)
        {
            var resultsPerDevice = new Result[Devices.Length];
            var program = _cl.CreateProgramWithBinary(
                _context.Value,
                Devices.Length,
                Devices,
                new[] { binaryLength },
                new[] { binary },
                resultsPerDevice,
                out var result);
            VerifyResult(result);
            VerifyResults(
                resultsPerDevice,
                (deviceResult, i) => new InvalidOperationException($"Error while creating program on device #{i}: {deviceResult}"));

            VerifyResult(_cl.BuildProgram(
                program,
                Devices.Length,
                Devices,
                null,
                null,
                IntPtr.Zero));

            return program;
        }

        private void SetKernelArgument(IntPtr kernel, int index, IntPtr buffer)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                VerifyResult(_cl.SetKernelArg(kernel, (uint)index, _intPtrSize, handle.AddrOfPinnedObject()));
            }
            finally
            {
                handle.Free();
            }
        }

        private IntPtr EnqueueMemoryMigration(IntPtr queue, IntPtr[] memoryObjects, bool toHost, IntPtr waitEvent)
        {
            var flags = toHost ? MemoryMigrationFlags.Host : MemoryMigrationFlags.None;
            VerifyResult(_cl.EnqueueMigrateMemObjects(
                queue,
                (uint)memoryObjects.Length,
                memoryObjects,
                flags,
                waitEvent == IntPtr.Zero ? 0u : 1,
                waitEvent == IntPtr.Zero ? null : new[] { waitEvent },
                out waitEvent));
            return waitEvent;
        }

        private IntPtr EnqueueTask(IntPtr queue, IntPtr kernel, IntPtr waitEvent)
        {
            VerifyResult(_cl.EnqueueTask(
                queue,
                kernel,
                waitEvent == IntPtr.Zero ? 0u : 1,
                waitEvent == IntPtr.Zero ? null : new[] { waitEvent },
                out waitEvent));
            return waitEvent;
        }

        #endregion

        public static void InitializeService(IServiceCollection services) =>
            services.AddSingleton(_ => NativeLibraryBuilder.Default.ActivateInterface<IOpenCl>("OpenCL"));

        public void Dispose()
        {
            foreach (var queue in _queues.Values) _cl.Finish(queue);

            var exceptions = new List<Exception>();

            foreach (var (name, handle) in _kernels)
            {
                var arguments = VerifyResults(
                    GetKernelBuffers(name).Select(_cl.ReleaseMemObject),
                    (result, _) => new InvalidOperationException($"Error releasing buffer for kernel '{name}': {result}"));
                if (arguments != null) exceptions.AddRange(arguments.InnerExceptions);

                var kernelReleaseResult = _cl.ReleaseKernel(handle);
                if (kernelReleaseResult != Result.Success)
                {
                    exceptions.Add(new InvalidOperationException($"Error releasing kernel '{name}': {kernelReleaseResult}"));
                }
            }

            var queues = _queues.ToList();
            var queueReleaseExceptions = VerifyResults(
                _queues.Values.Select(_cl.ReleaseCommandQueue),
                (result, index) => new InvalidOperationException($"Error releasing queue for device #{queues[index].Key}: {result}"));
            if (queueReleaseExceptions != null) exceptions.AddRange(queueReleaseExceptions.InnerExceptions);

            try
            {
                if (_context.IsValueCreated && _context.Value != IntPtr.Zero)
                {
                    VerifyResult(_cl.ReleaseContext(_context.Value));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            foreach (var exception in exceptions)
            {
                _logger.LogError(exception, "Error while disposing " + nameof(BinaryOpenCl) + ".");
            }

            GC.SuppressFinalize(this);
        }

        private IntPtr GetQueue(int queueIndex)
        {
            if (_queues.TryGetValue(queueIndex, out var queue)) return queue;
            throw new InvalidOperationException(
                $"There is no command queue for device #{queueIndex}. Please use {nameof(CreateCommandQueue)} to create one!");
        }

        private IntPtr GetKernel(string kernelName)
        {
            if (_kernels.TryGetValue(kernelName, out var kernel)) return kernel;
            throw new InvalidOperationException(
                $"The kernel '{kernelName}' does not exit. You can create a kernel with {nameof(CreateBinaryKernel)}.");
        }

        private List<IntPtr> GetKernelBuffers(string kernelName)
        {
            if (_kernelBuffers.TryGetValue(kernelName, out var kernel)) return kernel;
            throw new InvalidOperationException(
                $"The kernel '{kernelName}' does not exit. You can create a kernel with {nameof(CreateBinaryKernel)}.");
        }
    }
}
