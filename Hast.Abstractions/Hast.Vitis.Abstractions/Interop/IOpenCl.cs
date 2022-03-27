using AdvancedDLSupport;
using Hast.Vitis.Abstractions.Interop.Delegates.OpenCl;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using System;

namespace Hast.Vitis.Abstractions.Interop;

/// <summary>
/// ADL Interoperability interface for the OpenCL native library. The method documentation is copied from source
/// as-is <see href="https://www.khronos.org/registry/OpenCL/sdk/1.0/docs/man/xhtml/"/>.
/// </summary>
[NativeSymbols(Prefix = "cl")]
public interface IOpenCl : IDisposable
{
    /// <summary>
    /// Obtain the list of platforms available.
    /// </summary>
    /// <param name="numberOfEntries">
    /// The number of cl_platform_id entries that can be added to platforms. If platforms is not NULL, the
    /// num_entries must be greater than zero.
    /// </param>
    /// <param name="platforms">
    /// Returns a list of OpenCL platforms found. The cl_platform_id values returned in platforms can be used to
    /// identify a specific OpenCL platform. If platforms argument is NULL, this argument is ignored. The number of
    /// OpenCL platforms returned is the minimum of the value specified by num_entries or the number of OpenCL
    /// platforms available.
    /// </param>
    /// <param name="numberOfPlatforms">
    /// Returns the number of OpenCL platforms available. If num_platforms is NULL, this argument is ignored.
    /// </param>
    Result GetPlatformIDs(uint numberOfEntries, IntPtr[] platforms, out uint numberOfPlatforms);

    /// <summary>
    /// Get specific information about the OpenCL platform.
    /// </summary>
    /// <param name="platform">
    /// The platform ID returned by clGetPlatformIDs or can be NULL. If platform is NULL, the behavior is
    /// implementation-defined.
    /// </param>
    /// <param name="parameterName">
    /// An enumeration constant that identifies the platform information being queried. It can be one of the
    /// following values as specified in the table below.
    /// </param>
    /// <param name="parameterValueSize">
    /// Specifies the size in bytes of memory pointed to by param_value. This size in bytes must be greater than or
    /// equal to size of return type specified in the table below.
    /// </param>
    /// <param name="parameterValue">
    /// A pointer to memory location where appropriate values for a given param_value will be returned. Acceptable
    /// param_value values are listed in the table below. If param_value is NULL, it is ignored.
    /// </param>
    /// <param name="parameterValueSizeReturned">
    /// Returns the actual size in bytes of data being queried by param_value. If param_value_size_ret is NULL, it
    /// is ignored.
    /// </param>
    Result GetPlatformInfo(
        IntPtr platform,
        PlatformInformation parameterName,
        UIntPtr parameterValueSize,
        byte[] parameterValue,
        out UIntPtr parameterValueSizeReturned);

    /// <summary>
    /// Obtain the list of devices available on a platform.
    /// </summary>
    /// <param name="platform">
    /// Refers to the platform ID returned by clGetPlatformIDs or can be NULL. If platform is NULL, the behavior is
    /// implementation-defined.
    /// </param>
    /// <param name="deviceTypes">
    /// A bitfield that identifies the type of OpenCL device. The device_type can be used to query specific OpenCL
    /// devices or all OpenCL devices available. The valid values for device_type are specified in the following
    /// table.
    /// </param>
    /// <param name="numberOfEntries">
    /// The number of cl_device entries that can be added to devices. If devices is not NULL, the num_entries must
    /// be greater than zero.
    /// </param>
    /// <param name="devices">
    /// A list of OpenCL devices found. The cl_device_id values returned in devices can be used to identify a
    /// specific OpenCL device. If devices argument is NULL, this argument is ignored. The number of OpenCL devices
    /// returned is the minimum of the value specified by num_entries or the number of OpenCL devices whose type
    /// matches device_type.
    /// </param>
    /// <param name="numberOfDevicesReturned">
    /// The number of OpenCL devices available that match device_type. If num_devices is NULL, this argument is
    /// ignored.
    /// </param>
    Result GetDeviceIDs(IntPtr platform, DeviceTypes deviceTypes, uint numberOfEntries, IntPtr[] devices, out uint numberOfDevicesReturned);

    /// <summary>
    /// Creates an OpenCL context.
    /// </summary>
    /// <param name="properties">
    /// Specifies a list of context property names and their corresponding values. Each property name is immediately
    /// followed by the corresponding desired value. The list is terminated with 0. properties can be NULL in which
    /// case the platform that is selected is implementation-defined. The list of supported properties is described
    /// in the table below.
    /// </param>
    /// <param name="numberOfDevices">The number of devices specified in the devices argument.</param>
    /// <param name="devices">
    /// A pointer to a list of unique devices returned by clGetDeviceIDs for a platform.
    /// </param>
    /// <param name="notificationCallback">
    /// A callback function that can be registered by the application. This callback function will be used by the
    /// OpenCL implementation to report information on errors that occur in this context. This callback function may
    /// be called asynchronously by the OpenCL implementation. It is the application's responsibility to ensure that
    /// the callback function is thread-safe. If pfn_notify is NULL, no callback function is registered.
    /// </param>
    /// <param name="userData">
    /// Passed as the user_data argument when pfn_notify is called. user_data can be NULL.
    /// </param>
    /// <param name="errorCode">
    /// Returns an appropriate error code. If errcode_ret is NULL, no error code is returned.
    /// </param>
    IntPtr CreateContext(
        IntPtr properties,
        uint numberOfDevices,
        IntPtr[] devices,
        IntPtr notificationCallback,
        IntPtr userData,
        out Result errorCode);

    /// <summary>
    /// Decrement the context reference count.
    /// </summary>
    /// <param name="context">The context to release.</param>
    Result ReleaseContext(IntPtr context);

    /// <summary>
    /// Create a command-queue on a specific device.
    /// </summary>
    /// <param name="context">Must be a valid OpenCL context.</param>
    /// <param name="device">
    /// Must be a device associated with context. It can either be in the list of devices specified when context is
    /// created using clCreateContext or have the same device type as the device type specified when the context is
    /// created using clCreateContextFromType.
    /// </param>
    /// <param name="properties">
    /// Specifies a list of properties for the command-queue. This is a bit-field. Only command-queue properties
    /// specified in the table below can be set in properties; otherwise the value specified in properties is
    /// considered to be not valid.
    /// </param>
    /// <param name="errorCode">
    /// Returns an appropriate error code. If errcode_ret is NULL, no error code is returned.
    /// </param>
    IntPtr CreateCommandQueue(IntPtr context, IntPtr device, CommandQueueProperties properties, out Result errorCode);

    /// <summary>
    /// Decrements the command_queue reference count.
    /// </summary>
    /// <param name="commandQueue">Specifies the command-queue to release.</param>
    Result ReleaseCommandQueue(IntPtr commandQueue);

    /// <summary>
    /// Creates a program object for a context, and loads specified binary data into the program object.
    /// </summary>
    /// <param name="context">Must be a valid OpenCL context.</param>
    /// <param name="numberOfDevices">
    /// A pointer to a list of devices that are in context. device_list must be a non-NULL value. The binaries are
    /// loaded for devices specified in this list.
    /// </param>
    /// <param name="deviceList">
    /// The number of devices listed in device_list. The devices associated with the program object will be the list
    /// of devices specified by device_list. The list of devices specified by device_list must be devices associated
    /// with context.
    /// </param>
    /// <param name="lengths">
    /// An array of the size in bytes of the program binaries to be loaded for devices specified by device_list.
    /// </param>
    /// <param name="binaries">
    /// An array of pointers to program binaries to be loaded for devices specified by device_list. For each device
    /// given by device_list[i], the pointer to the program binary for that device is given by binaries[i] and the
    /// length of this corresponding binary is given by lengths[i]. lengths[i] cannot be zero and binaries[i] cannot
    /// be a NULL pointer. The program binaries specified by binaries contain the bits that describe the program
    /// executable that will be run on the device(s) associated with context. The program binary can consist of
    /// either or both of device-specific executable(s), and/or implementation-specific intermediate representation
    /// (IR) which will be converted to the device-specific executable.
    /// </param>
    /// <param name="binaryStatus">
    /// Returns whether the program binary for each device specified in device_list was loaded successfully or not.
    /// It is an array of num_devices entries and returns CL_SUCCESS in binary_status[i] if binary was successfully
    /// loaded for device specified by device_list[i]; otherwise returns CL_INVALID_VALUE if lengths[i] is zero or
    /// if binaries[i] is a NULL value or CL_INVALID_BINARY in binary_status[i] if program binary is not a valid
    /// binary for the specified device. If binary_status is NULL, it is ignored.
    /// </param>
    /// <param name="errorCode">
    /// Returns an appropriate error code. If errcode_ret is NULL, no error code is returned.
    /// </param>
    IntPtr CreateProgramWithBinary(
        IntPtr context,
        int numberOfDevices,
        IntPtr[] deviceList,
        int[] lengths,
        IntPtr[] binaries,
        Result[] binaryStatus,
        out Result errorCode);

    /// <summary>
    /// Builds (compiles and links) a program executable from the program source or binary.
    /// </summary>
    /// <param name="program">The program object.</param>
    /// <param name="numberOfDevices">
    /// A pointer to a list of devices that are in program. If device_list is NULL value, the program executable is
    /// built for all devices associated with program for which a source or binary has been loaded. If device_list
    /// is a non-NULL value, the program executable is built for devices specified in this list for which a source
    /// or binary has been loaded.
    /// </param>
    /// <param name="deviceList">The number of devices listed in device_list.</param>
    /// <param name="options">
    /// A pointer to a string that describes the build options to be used for building the program executable. The
    /// list of supported options is described in "Build Options" below.
    /// </param>
    /// <param name="notify">
    /// A function pointer to a notification routine. The notification routine is a callback function that an
    /// application can register and which will be called when the program executable has been built (successfully
    /// or unsuccessfully). If pfn_notify is not NULL, clBuildProgram does not need to wait for the build to
    /// complete and can return immediately. If pfn_notify is NULL, clBuildProgram does not return until the build
    /// has completed. This callback function may be called asynchronously by the OpenCL implementation. It is the
    /// application's responsibility to ensure that the callback function is thread-safe.
    /// </param>
    /// <param name="userData">
    /// Passed as an argument when pfn_notify is called. user_data can be NULL.
    /// </param>
    Result BuildProgram(IntPtr program, int numberOfDevices, IntPtr[] deviceList, string options, BuildCallback notify, IntPtr userData);

    /// <summary>
    /// Creates a kernel object.
    /// </summary>
    /// <param name="program">A program object with a successfully built executable.</param>
    /// <param name="kernelName">
    /// A function name in the program declared with the __kernel qualifier.
    /// </param>
    /// <param name="errorCode">
    /// Returns an appropriate error code. If errcode_ret is NULL, no error code is returned.
    /// </param>
    IntPtr CreateKernel(IntPtr program, string kernelName, out Result errorCode);

    /// <summary>
    /// Decrements the kernel reference count.
    /// </summary>
    Result ReleaseKernel(IntPtr kernel);

    /// <summary>
    /// Creates a buffer object.
    /// </summary>
    /// <param name="context">A valid OpenCL context used to create the buffer object.</param>
    /// <param name="flags">
    /// A bit-field that is used to specify allocation and usage information such as the memory arena that should be
    /// used to allocate the buffer object and how it will be used.
    /// </param>
    /// <param name="size">The size in bytes of the buffer memory object to be allocated.</param>
    /// <param name="hostPointer">
    /// A pointer to the buffer data that may already be allocated by the application. The size of the buffer that
    /// host_ptr points to must be greater than or equal to the size bytes.
    /// </param>
    /// <param name="errorCode">
    /// Returns an appropriate error code. If errcode_ret is NULL, no error code is returned.
    /// </param>
    IntPtr CreateBuffer(IntPtr context, MemoryFlags flags, int size, IntPtr hostPointer, out Result errorCode);

    /// <summary>
    /// Decrements the memory object reference count.
    /// </summary>
    Result ReleaseMemObject(IntPtr buffer);

    /// <summary>
    /// Used to set the argument value for a specific argument of a kernel.
    /// </summary>
    /// <param name="kernel">A valid kernel object.</param>
    /// <param name="argumentIndex">
    /// The argument index. Arguments to the kernel are referred by indices that go from 0 for the leftmost argument
    /// to n - 1, where n is the total number of arguments declared by a kernel.
    /// </param>
    /// <param name="argumentSize">
    /// Specifies the size of the argument value. If the argument is a memory object, the size is the size of the
    /// buffer or image object type. For arguments declared with the __local qualifier, the size specified will be
    /// the size in bytes of the buffer that must be allocated for the __local argument. If the argument is of type
    /// sampler_t, the arg_size value must be equal to sizeof(cl_sampler). For all other arguments, the size will be
    /// the size of argument type.
    /// </param>
    /// <param name="argumentValue">
    /// A pointer to data that should be used as the argument value for argument specified by arg_index. The
    /// argument data pointed to by arg_value is copied and the arg_value pointer can therefore be reused by the
    /// application after clSetKernelArg returns. The argument value specified is the value used by all API calls
    /// that enqueue kernel (clEnqueueNDRangeKernel and clEnqueueTask) until the argument value is changed by a call
    /// to clSetKernelArg for kernel.
    /// </param>
    Result SetKernelArg(IntPtr kernel, uint argumentIndex, UIntPtr argumentSize, IntPtr argumentValue);

    /// <summary>
    /// Enqueues a command to indicate which device a set of memory objects should be associated with.
    /// </summary>
    /// <param name="commandQueue">
    /// A valid command-queue. The specified set of memory objects in mem_objects will be migrated to the OpenCL
    /// device associated with command_queue or to the host if the CL_MIGRATE_MEM_OBJECT_HOST has been specified.
    /// </param>
    /// <param name="numberOfMemoryObjects">The number of memory objects specified in mem_objects.</param>
    /// <param name="memoryObjects">A pointer to a list of memory objects.</param>
    /// <param name="memoryMigrationFlags">
    /// A bit-field that is used to specify migration options.
    /// </param>
    /// <param name="numberOfEventsInWaitList">The length of <paramref name="eventWaitList"/>.</param>
    /// <param name="eventWaitList">
    /// Specify events that need to complete before this particular command can be executed. If event_wait_list is
    /// NULL, then this particular command does not wait on any event to complete. If event_wait_list is NULL,
    /// num_events_in_wait_list must be 0. If event_wait_list is not NULL, the list of events pointed to by
    /// event_wait_list must be valid and num_events_in_wait_list must be greater than 0. The events specified in
    /// event_wait_list act as synchronization points. The context associated with events in event_wait_list and
    /// command_queue must be the same. The memory associated with event_wait_list can be reused or freed after the
    /// function returns.
    /// </param>
    /// <param name="waitEvent">
    /// Returns an event object that identifies this particular command and can be used to query or queue a wait for
    /// this particular command to complete. event can be NULL in which case it will not be possible for the
    /// application to query the status of this command or queue a wait for this command to complete. If the
    /// event_wait_list and the event arguments are not NULL, the event argument should not refer to an element of
    /// the event_wait_list array.
    /// </param>
    Result EnqueueMigrateMemObjects(
        IntPtr commandQueue,
        uint numberOfMemoryObjects,
        IntPtr[] memoryObjects,
        MemoryMigrationFlags memoryMigrationFlags,
        uint numberOfEventsInWaitList,
        IntPtr[] eventWaitList,
        out IntPtr waitEvent);

    /// <summary>
    /// Enqueues a command to execute a kernel on a device.
    /// </summary>
    /// <param name="commandQueue">
    /// A valid command-queue. The kernel will be queued for execution on the device associated with command_queue.
    /// </param>
    /// <param name="kernel">
    /// A valid kernel object. The OpenCL context associated with kernel and command_queue must be the same.
    /// </param>
    /// <param name="numberOfEventsInWaitList">The length of <paramref name="eventWaitList"/>.</param>
    /// <param name="eventWaitList">
    /// Specify events that need to complete before this particular command can be executed. If event_wait_list is
    /// NULL, then this particular command does not wait on any event to complete. If event_wait_list is NULL,
    /// num_events_in_wait_list must be 0. If event_wait_list is not NULL, the list of events pointed to by
    /// event_wait_list must be valid and num_events_in_wait_list must be greater than 0. The events specified in
    /// event_wait_list act as synchronization points. The context associated with events in event_wait_list and
    /// command_queue must be the same. The memory associated with event_wait_list can be reused or freed after the
    /// function returns.
    /// </param>
    /// <param name="waitEvent">
    /// Returns an event object that identifies this particular kernel execution instance. Event objects are unique
    /// and can be used to identify a particular kernel execution instance later on. If event is NULL, no event will
    /// be created for this kernel execution instance and therefore it will not be possible for the application to
    /// query or queue a wait for this particular kernel execution instance.
    /// </param>
    Result EnqueueTask(IntPtr commandQueue, IntPtr kernel, uint numberOfEventsInWaitList, IntPtr[] eventWaitList, out IntPtr waitEvent);

    /// <summary>
    /// Blocks until all previously queued OpenCL commands in a command-queue are issued to the associated device
    /// and have completed.
    /// </summary>
    Result Finish(IntPtr commandQueue);
}
