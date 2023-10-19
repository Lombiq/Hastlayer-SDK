using System;
using System.Diagnostics.CodeAnalysis;

// Some of the enums are taken from OpenCL.NET:
// PlatformInformation is sourced from
// https://github.com/lecode-official/opencl-dotnet/blob/master/OpenCl.DotNetCore.Interop/Platforms/PlatformInformation.cs
// Result is sourced from https://github.com/lecode-official/opencl-dotnet/blob/master/OpenCl.DotNetCore.Interop/Result.cs
// DeviceType is sourced from https://github.com/lecode-official/opencl-dotnet/blob/master/OpenCl.DotNetCore.Interop/Devices/DeviceType.cs

// The rest are created using a similar format from the official C API documentation at the Khronos OpenCL Registry:
// MemoryFlag is described at https://www.khronos.org/registry/OpenCL/sdk/1.0/docs/man/xhtml/clCreateBuffer.html
// MemoryMigrationFlag at https://www.khronos.org/registry/OpenCL/sdk/1.2/docs/man/xhtml/clEnqueueMigrateMemObjects.html
// CommandQueueProperty at https://www.khronos.org/registry/OpenCL/sdk/1.0/docs/man/xhtml/clCreateCommandQueue.html

namespace Hast.Vitis.Interop.Enums;

/// <summary>
/// Represents an enumeration for the different types of information that can be queried from an OpenCl platform.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Not applicable.")]
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The external type is uint.")]
public enum PlatformInformation : uint
{
    /// <summary>
    /// OpenCL profile string. Returns the profile name supported by the implementation. The profile name returned can
    /// be one of the following strings: FULL_PROFILE - if the implementation supports the OpenCL specification
    /// (functionality defined as part of the core specification and does not require any extensions to be supported).
    /// EMBEDDED_PROFILE - if the implementation supports the OpenCL embedded profile. The embedded profile is defined
    /// to be a subset for each version of OpenCL.
    /// </summary>
    Profile = 0x_0900,

    /// <summary>
    /// OpenCL version string. Returns the OpenCL version supported by the implementation. This version string has the
    /// following format: OpenCL{space}{major_version}.{minor_version}{space}{platform-specific information} The
    /// major_version.minor_version value returned will be 1.0.
    /// </summary>
    Version = 0x_0901,

    /// <summary>
    /// Platform name string.
    /// </summary>
    Name = 0x_0902,

    /// <summary>
    /// Platform vendor string.
    /// </summary>
    Vendor = 0x_0903,

    /// <summary>
    /// Returns a space-separated list of extension names (the extension names themselves do not contain any
    /// spaces) supported by the platform. Extensions defined here must be supported by all devices associated with this
    /// platform.
    /// </summary>
    Extensions = 0x_0904,
}

/// <summary>
/// Represents an enumeration for the result codes that are returned by the native OpenCL functions.
/// </summary>
public enum Result
{
    Success = 0,
    DeviceNotFound = -1,
    DeviceNotAvailable = -2,
    CompilerNotAvailable = -3,
    MemObjectAllocationFailure = -4,
    OutOfResources = -5,
    OutOfHostMemory = -6,
    ProfilingInformationNotAvailable = -7,
    MemCopyOverlap = -8,
    ImageFormatMismatch = -9,
    ImageFormatNotSupported = -10,
    BuildProgramFailure = -11,
    MapFailure = -12,
    InvalidValue = -30,
    InvalidDeviceType = -31,
    InvalidPlatform = -32,
    InvalidDevice = -33,
    InvalidContext = -34,
    InvalidQueueProperties = -35,
    InvalidCommandQueue = -36,
    InvalidHostPtr = -37,
    InvalidMemObject = -38,
    InvalidImageFormatDescriptor = -39,
    InvalidImageSize = -40,
    InvalidSampler = -41,
    InvalidBinary = -42,
    InvalidBuildOptions = -43,
    InvalidProgram = -44,
    InvalidProgramExecutable = -45,
    InvalidKernelName = -46,
    InvalidKernelDefinition = -47,
    InvalidKernel = -48,
    InvalidArgIndex = -49,
    InvalidArgValue = -50,
    InvalidArgSize = -51,
    InvalidKernelArgs = -52,
    InvalidWorkDimension = -53,
    InvalidWorkGroupSize = -54,
    InvalidWorkItemSize = -55,
    InvalidGlobalOffset = -56,
    InvalidEventWaitList = -57,
    InvalidEvent = -58,
    InvalidOperation = -59,
    InvalidGLObject = -60,
    InvalidBufferSize = -61,
    InvalidMipLevel = -62,
    InvalidGlobalWorkSize = -63,
    InvalidProperty = -64,
    InvalidImageDescriptor = -65,
    InvalidCompilerOptions = -66,
    InvalidLinkerOptions = -67,
    InvalidDevicePartitionCount = -68,
    InvalidPipeSize = -69,
    InvalidDeviceQueue = -70,
    PlatformNotFoundKhr = -1_001,
    DevicePartitionFailedExt = -1_057,
    InvalidPartitionCountExt = -1_058,
    InvalidPartitionNameExt = -1_059,
}

/// <summary>
/// A bitfield that identifies the type of OpenCL device. The device_type can be used to query specific OpenCL devices
/// or all OpenCL devices available.
/// </summary>
[Flags]
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The external type is ulong.")]
public enum DeviceTypes : ulong
{
    /// <summary>
    /// An OpenCL device that is the host processor. The host processor runs the OpenCL implementations and is a single
    /// or multi-core CPU.
    /// </summary>
    Cpu = 1 << 1,

    /// <summary>
    /// An OpenCL device that is a GPU. By this we mean that the device can also be used to accelerate a 3D API such as
    /// OpenGL or DirectX.
    /// </summary>
    Gpu = 1 << 2,

    /// <summary>
    /// Dedicated OpenCL accelerators (for example the IBM CELL Blade). These devices communicate with the host
    /// processor using a peripheral interconnect such as PCIe.
    /// </summary>
    Accelerator = 1 << 3,

    /// <summary>
    /// The default OpenCL device in the system.
    /// </summary>
    Default = 1 << 0,
}

/// <summary>
/// A bit-field that is used to specify allocation and usage information such as the memory arena that should be used to
/// allocate the buffer object and how it will be used.
/// </summary>
[Flags]
[SuppressMessage(
    "Minor Code Smell",
    "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes",
    Justification = "This is the name in the original library and changing it would be confusing.")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Same.")]
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The external type is ulong.")]
public enum MemoryFlags : ulong
{
    /// <summary>
    /// This flag specifies that the memory object will be read and written by a kernel. This is the default.
    /// </summary>
    ReadWrite = 1 << 0,

    /// <summary>
    /// This flags specifies that the memory object will be written but not read by a kernel. Reading from a buffer or
    /// image object created with <see cref="WriteOnly"/> inside a kernel is undefined.
    /// </summary>
    WriteOnly = 1 << 1,

    /// <summary>
    /// This flag specifies that the memory object is a read-only memory object when used inside a kernel. Writing to a
    /// buffer or image object created with <see cref="ReadOnly"/> inside a kernel is undefined.
    /// </summary>
    ReadOnly = 1 << 2,

    /// <summary>
    /// This flag is valid only if host_ptr is not NULL. If specified, it indicates that the application wants the
    /// OpenCL implementation to use memory referenced by host_ptr as the storage bits for the memory object. OpenCL
    /// implementations are allowed to cache the buffer contents pointed to by host_ptr in device memory. This cached
    /// copy can be used when kernels are executed on a device. The result of OpenCL commands that operate on multiple
    /// buffer objects created with the same host_ptr or overlapping host regions is considered to be undefined.
    /// </summary>
    UseHostPointer = 1 << 3,

    /// <summary>
    /// This flag specifies that the application wants the OpenCL implementation to allocate memory from host accessible
    /// memory. <see cref="AllocateHostPointer"/> and <see cref="UseHostPointer"/> are mutually exclusive.
    /// </summary>
    AllocateHostPointer = 1 << 4,

    /// <summary>
    /// This flag is valid only if host_ptr is not NULL. If specified, it indicates that the application wants the
    /// OpenCL implementation to allocate memory for the memory object and copy the data from memory referenced by
    /// host_ptr. <see cref="CopyHostPointer"/> and <see cref="UseHostPointer"/> are mutually exclusive. <see
    /// cref="CopyHostPointer"/> can be used with <see cref="AllocateHostPointer"/> to initialize the contents of the
    /// cl_mem object allocated using host-accessible (e.g. PCIe) memory.
    /// </summary>
    CopyHostPointer = 1 << 5,

    /// <summary>
    /// Make clCreateBuffer to interpret host_ptr argument as cl_mem_ext_ptr_t.
    /// </summary>
    /// <remarks>
    /// <para>See <see href="https://github.com/Xilinx/XRT/blob/master/src/include/1_2/CL/cl_ext_xilinx.h"/>.</para>
    /// </remarks>
    ExtensionXilinxPointer = 1ul << 31,

    // Blame Xilinx.
#pragma warning disable CA1069 // Enums values should not be duplicated
    ExtensionXilinxTopology = 1ul << 31,
#pragma warning restore CA1069 // Enums values should not be duplicated
}

/// <summary>
/// A bit-field that is used to specify migration options.
/// </summary>
[Flags]
[SuppressMessage(
    "Minor Code Smell",
    "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes",
    Justification = "This is the name in the original library and changing it would be confusing.")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Same.")]
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The external type is ulong.")]
public enum MemoryMigrationFlags : ulong
{
    /// <summary>
    /// This flag indicates that <see cref="Host"/> is not used, so the specified set of memory objects are to be
    /// migrated to the device.
    /// </summary>
    None = 0,

    /// <summary>
    /// This flag indicates that the specified set of memory objects are to be migrated to the host, regardless of the
    /// target command-queue.
    /// </summary>
    Host = 1 << 0,

    /// <summary>
    /// This flag indicates that the contents of the set of memory objects are undefined after migration. The specified
    /// set of memory objects are migrated to the device associated with command_queue without incurring the overhead of
    /// migrating their contents.
    /// </summary>
    ContentUndefined = 1 << 1,
}

[Flags]
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The external type is ulong.")]
public enum CommandQueueProperties : ulong
{
    None = 0,

    /// <summary>
    /// Determines whether the commands queued in the command-queue are executed in-order or out-of-order. If set, the
    /// commands in the command-queue are executed out-of-order. Otherwise, commands are executed in-order.
    /// </summary>
    OutOfOrderExecutionModeEnable = 1 << 0,

    /// <summary>
    /// Enable or disable profiling of commands in the command-queue. If set, the profiling of commands is enabled.
    /// Otherwise profiling of commands is disabled. See clGetEventProfilingInfo for more information.
    /// </summary>
    ProfilingEnable = 1 << 1,
}
