using Hast.SDAccel.Abstractions.Interop;
using Hast.SDAccel.Abstractions.Interop.Enums.OpenCl;
using Hast.SDAccel.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AdvancedDLSupport;
using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Hast.SDAccel.Abstractions.Services
{
    [IDependencyInitializer(nameof(InitializeService))]
    public class BinaryOpenCl : IBinaryOpenCl
    {
        #region Fields and properties

        private readonly IOpenCl _cl;
        private readonly IntPtr[] _devices;
        private readonly IntPtr _context = IntPtr.Zero;
        private readonly UIntPtr IntPtrSize = new UIntPtr((uint)Marshal.SizeOf(IntPtr.Zero));

        public int DeviceCount => _devices.Length;

        Dictionary<int, IntPtr> Queues { get; } = new Dictionary<int, IntPtr>();
        Dictionary<string, IntPtr> Kernels { get; } = new Dictionary<string, IntPtr>();
        private Dictionary<string, List<IntPtr>> KernelBuffers { get; set; } = new Dictionary<string, List<IntPtr>>();

        #endregion


        #region Constructors

        public BinaryOpenCl(IOpenCl cl, IOpenClConfiguration configuration)
        {
            _cl = cl;

            _devices = GetDeviceHandlesOfVendor(configuration.VendorName, configuration.DeviceType);
            if (_devices.Any())
            {
                _context = _cl.CreateContext(IntPtr.Zero, (uint)_devices.Length, _devices.ToArray(), IntPtr.Zero, IntPtr.Zero, out Result result);
                VerifyResult(result);
            }
        }

        #endregion


        #region Methods

        static void VerifyResult(Result err) { if (err != Result.Success) throw new Exception($"ERROR STATUS: {err}"); }

        static AggregateException VerifyResults(IEnumerable<Result> results, Func<Result, int, Exception> mapper)
        {
            var errors = results
                .Select((Result, Index) => (Result, Index))
                .Where(x => x.Result != Result.Success)
                .Select(x => mapper(x.Result, x.Index))
                .ToList();
            return errors.Count > 0 ? new AggregateException(errors) : null;
        }

        IEnumerable<IntPtr> GetPlatformHandles(string vendorName = null)
        {
            VerifyResult(_cl.GetPlatformIDs(0, null, out uint count));

            IntPtr[] platforms = new IntPtr[count];
            VerifyResult(_cl.GetPlatformIDs(count, platforms, out _));

            if (string.IsNullOrEmpty(vendorName)) return platforms;

            return platforms.Where(platform =>
            {
                VerifyResult(_cl.GetPlatformInfo(platform, PlatformInformation.Name, UIntPtr.Zero, null, out UIntPtr size));
                var output = new byte[size.ToUInt32()];
                VerifyResult(_cl.GetPlatformInfo(platform, PlatformInformation.Name, size, output, out _));

                return vendorName == Encoding.UTF8.GetString(output, 0, output.Length).TrimEnd('\0');
            });
        }

        IntPtr[] GetDeviceHandles(IntPtr platform, DeviceType deviceType)
        {
            VerifyResult(_cl.GetDeviceIDs(platform, deviceType, 0, null, out uint numberOfAvailableDevices));

            IntPtr[] devicePointers = new IntPtr[numberOfAvailableDevices];
            VerifyResult(_cl.GetDeviceIDs(platform, deviceType, numberOfAvailableDevices, devicePointers, out _));

            return devicePointers;
        }

        IntPtr[] GetDeviceHandlesOfVendor(string vendorName, DeviceType deviceType)
        {
            foreach (var platform in GetPlatformHandles(vendorName))
            {
                var devices = GetDeviceHandles(platform, deviceType);
                if (devices.Length > 0) return devices;
            }

            throw new Exception($"Failed to find '{vendorName}' platform that has '{deviceType}' type devices.");
        }

        public void CreateCommandQueue(int deviceIndex, CommandQueueProperty properties = CommandQueueProperty.ProfilingEnable)
        {
            var queue = _cl.CreateCommandQueue(_context, _devices[deviceIndex], properties, out Result result);
            VerifyResult(result);
            Queues[deviceIndex] = queue;
        }

        IntPtr CreateProgramWithBinary(IntPtr binary, int binaryLength)
        {
            var resultsPerDevice = new Result[_devices.Length];
            var program = _cl.CreateProgramWithBinary(
                _context,
                _devices.Length,
                _devices, 
                new[] { binaryLength },
                new[] { binary },
                resultsPerDevice,
                out Result result);
            VerifyResult(result);
            VerifyResults(resultsPerDevice,
                (deviceResult, i) => new Exception($"Error while creating program on device #{i}: {deviceResult}"));

            VerifyResult(_cl.BuildProgram(
                program,
                _devices.Length,
                _devices,
                null,
                null,
                IntPtr.Zero));

            return program;
        }

        public void CreateBinaryKernel(byte[] binary, string kernelName)
        {
            if (Kernels.ContainsKey(kernelName)) return;
            
            unsafe
            {
                fixed (byte* pointer = binary)
                {
                    var program = CreateProgramWithBinary((IntPtr) pointer, binary.Length);
                    
                    var kernel = _cl.CreateKernel(program, kernelName, out Result result);
                    VerifyResult(result);
                    Kernels[kernelName] = kernel;
                    KernelBuffers[kernelName] = new List<IntPtr>();
                }   
            }
        }

        IntPtr CreateBuffer(IntPtr hostPointer, int hostBytes)
        {
            var memoryFlags = MemoryFlag.UseHostPointer | MemoryFlag.ReadWrite;
            var buffer = _cl.CreateBuffer(_context, memoryFlags, hostBytes, hostPointer, out Result result);
            VerifyResult(result);
            return buffer;
        }

        void SetKernelArgument(IntPtr kernel, int index, IntPtr buffer)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                VerifyResult(_cl.SetKernelArg(kernel, (uint)index, IntPtrSize, handle.AddrOfPinnedObject()));
            }
            finally
            {
                handle.Free();
            }
        }

        public IntPtr SetKernelArgumentWithNewBuffer(string kernelName, int index, Span<byte> data)
        {
            unsafe
            {
                fixed (byte* handle = data)
                {
                    var buffer = CreateBuffer((IntPtr)handle, data.Length);
                    SetKernelArgument(Kernels[kernelName], index, buffer);
                    KernelBuffers[kernelName].Add(buffer);
                    return buffer;
                }
            }
        }

        IntPtr EnqueueMemoryMigration(IntPtr queue, IntPtr[] memoryObjects, bool toHost, IntPtr waitEvent)
        {
            var flags = toHost ? MemoryMigrationFlag.Host : MemoryMigrationFlag.Device;
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

        IntPtr EnqueueTask(IntPtr queue, IntPtr kernel, IntPtr waitEvent)
        {
            VerifyResult(_cl.EnqueueTask(
                queue,
                kernel,
                waitEvent == IntPtr.Zero ? 0u : 1,
                waitEvent == IntPtr.Zero ? null : new[] { waitEvent }, 
                out waitEvent));
            return waitEvent;
        }

        public void LaunchKernel(int deviceIndex, string kernelName, IntPtr[] buffers, bool copyBack = true)
        {
            var queue = Queues[deviceIndex];
            var kernel = Kernels[kernelName];
            var waitEvent = IntPtr.Zero;

            waitEvent = EnqueueMemoryMigration(queue, buffers, false, waitEvent);
            waitEvent = EnqueueTask(queue, kernel, waitEvent);
            if (copyBack) EnqueueMemoryMigration(queue, buffers, true, waitEvent);
        }

        public async Task AwaitDevice(int deviceIndex)
        {
            var queue = Queues[deviceIndex];
            VerifyResult(await Task.Run(() => _cl.Finish(queue)));
        }

        #endregion

        public static void InitializeService(IServiceCollection services)
        {
            services.AddSingleton(_ => NativeLibraryBuilder.Default.ActivateInterface<IOpenCl>("OpenCL"));
        }

        public void Dispose()
        {
            foreach (var queue in Queues.Values) _cl.Finish(queue);

            var exceptions = new List<Exception>();
            
            foreach (var (name, handle) in Kernels)
            {
                var arguments = VerifyResults(
                    KernelBuffers[name].Select(_cl.ReleaseMemObject), 
                    (result, _) => new Exception($"Error releasing buffer for kernel '{name}': {result}"));
                if (arguments != null) exceptions.AddRange(arguments.InnerExceptions);
                
                var kernelReleaseResult = _cl.ReleaseKernel(handle);
                if (kernelReleaseResult != Result.Success)
                {
                    exceptions.Add(new Exception($"Error releasing kernel '{name}': {kernelReleaseResult}"));
                }
            }

            var queues = Queues.ToList();
            var queueReleaseExceptions = VerifyResults(
                Queues.Values.Select(_cl.ReleaseCommandQueue),
                (result, index) => new Exception($"Error releasing queue for device #{queues[index].Key}: {result}"));
            if (queueReleaseExceptions != null) exceptions.AddRange(queueReleaseExceptions.InnerExceptions);

            try { VerifyResult(_cl.ReleaseContext(_context)); } catch (Exception ex) { exceptions.Add(ex); }

            if (exceptions.Count > 0)
            {
                throw  new AggregateException($"Error while disposing {nameof(BinaryOpenCl)}.", exceptions);
            }
        }
    }
}
