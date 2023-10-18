using Castle.DynamicProxy;
using Hast.Common.Extensibility.Pipeline;
using Hast.Common.Extensions;
using Hast.Common.Services;
using Hast.Communication.Exceptions;
using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.SimpleMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Communication;

public class MemberInvocationHandlerFactory : IMemberInvocationHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MemberInvocationHandlerFactory> _logger;

    public event EventHandler<ServiceEventArgs<IMemberHardwareExecutionContext>> MemberExecutedOnHardware;

    public event EventHandler<ServiceEventArgs<IMemberInvocationContext>> MemberInvoking;

    public MemberInvocationHandlerFactory(
        IServiceProvider serviceProvider,
        ILogger<MemberInvocationHandlerFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public MemberInvocationHandler CreateMemberInvocationHandler(
        IHardwareRepresentation hardwareRepresentation,
        object target,
        IProxyGenerationConfiguration configuration) =>
        invocation =>
        {
            var methodAsynchronicity = GetMethodAsynchronicity(invocation);

            if (methodAsynchronicity == MethodAsynchronicity.AsyncFunction)
            {
                throw new NotSupportedException("Only async methods that return a Task, not Task<T>, are supported.");
            }

            if (methodAsynchronicity == MethodAsynchronicity.AsyncAction)
            {
                invocation.ReturnValue = InvocationHandlerAsync(hardwareRepresentation, configuration, invocation, methodAsynchronicity);
            }
            else
            {
                InvocationHandlerAsync(hardwareRepresentation, configuration, invocation, methodAsynchronicity).Wait();
            }
        };

    private async Task InvocationHandlerAsync(
        IHardwareRepresentation hardwareRepresentation,
        IProxyGenerationConfiguration configuration,
        IInvocation invocation,
        MethodAsynchronicity methodAsynchronicity)
    {
        using var scope = _serviceProvider.CreateScope();
        var memoryResourceCheckers = scope.ServiceProvider.GetService<IEnumerable<IMemoryResourceChecker>>();

        // Although it says Method it can also be a property.
        var memberFullName = invocation.Method.GetFullName();

        var invocationContext = new MemberInvocationContext
        {
            Invocation = invocation,
            MemberFullName = memberFullName,
            HardwareRepresentation = hardwareRepresentation,
        };

        MemberInvoking?.Invoke(this, new ServiceEventArgs<IMemberInvocationContext>(invocationContext));

        scope
            .ServiceProvider
            .GetService<IEnumerable<IMemberInvocationPipelineStep>>()
            .InvokePipelineSteps(step =>
                invocationContext.HardwareExecutionIsCancelled = step.CanContinueHardwareExecution(invocationContext));

        if (!invocationContext.HardwareExecutionIsCancelled)
        {
            var hardwareMembers = hardwareRepresentation.HardwareDescription.HardwareEntryPointNamesToMemberIdMappings;
            var memberNameAlternates = new HashSet<string>(hardwareMembers.Keys.SelectMany(member => member.GetMemberNameAlternates()));
            invocationContext.HardwareExecutionIsCancelled =
                !hardwareMembers.ContainsKey(memberFullName) && !memberNameAlternates.Contains(memberFullName);
        }

        if (invocationContext.HardwareExecutionIsCancelled)
        {
            await OnCancelledAsync(invocation, invocationContext, methodAsynchronicity);

            return;
        }

        var communicationChannelName = configuration.CommunicationChannelName;
        var deviceManifest = hardwareRepresentation.DeviceManifest;

        if (string.IsNullOrEmpty(communicationChannelName))
        {
            communicationChannelName = deviceManifest.DefaultCommunicationChannelName;
        }

        if (!deviceManifest.SupportedCommunicationChannelNames.Contains(communicationChannelName))
        {
            throw new NotSupportedException(
                "The configured communication channel \"" + communicationChannelName +
                "\" is not supported by the current device.");
        }

        var memory = (SimpleMemory)invocation.Arguments.SingleOrDefault(argument => argument is SimpleMemory) ??
            throw new NotSupportedException(
                $"Only SimpleMemory-using implementations are supported for hardware execution. The " +
                $"{nameof(invocation)} didn't include a SimpleMemory argument.");

        if (memoryResourceCheckers.Check(memory, hardwareRepresentation) is { } problem)
        {
            var exception = new InvalidOperationException(
                $"The input is too large to fit into the device's memory. The input is " +
                $"{problem.MemoryByteCount} bytes, the available memory is " +
                $"{problem.AvailableByteCount} bytes. (Reported by {problem.GetType().FullName}.) " +
                $"{problem.Message}");
            foreach (var (key, value) in problem.AdditionalData) exception.Data[key] = value;
            throw exception;
        }

        SimpleMemory softMemory = null;

        if (configuration.VerifyHardwareResults)
        {
            softMemory = SimpleMemory.CreateSoftwareMemory(memory.CellCount);
            var memoryBytes = new SimpleMemoryAccessor(memory).Get();
            memoryBytes.CopyTo(new SimpleMemoryAccessor(softMemory).Get());

            var memoryArgumentIndex = invocation.Arguments
                .Select((argument, index) => new
                {
                    Argument = argument,
                    Index = index,
                })
                .Single(argument => argument.Argument is SimpleMemory)
                .Index;
            invocation.SetArgumentValue(memoryArgumentIndex, softMemory);

            var softwareExecutionStopwatch = Stopwatch.StartNew();
            // This needs to happen before the awaited Execute() call below, otherwise the Task in ReturnValue wouldn't
            // be the original one any more.
            invocation.Proceed();
            softwareExecutionStopwatch.Stop();
            invocationContext.SoftwareExecutionInformation = new SoftwareExecutionInformation
            {
                SoftwareExecutionTimeMilliseconds = softwareExecutionStopwatch.ElapsedMilliseconds,
            };

            if (methodAsynchronicity == MethodAsynchronicity.AsyncAction)
            {
                await (Task)invocation.ReturnValue;
            }
        }

        // At this point we checked that the hardware entry point does have a mapping.
        var memberId = hardwareRepresentation
            .HardwareDescription
            .HardwareEntryPointNamesToMemberIdMappings[memberFullName];

        var communicationService = scope
            .ServiceProvider
            .GetService<ICommunicationServiceSelector>()
            .GetCommunicationService(communicationChannelName) ?? throw new InvalidOperationException(
                $"No communication service was found for the channel \"{communicationChannelName}\".");

        _logger.LogInformation("Starting communication service execution...");
        invocationContext.HardwareExecutionInformation = await communicationService
            .ExecuteAsync(
                memory,
                memberId,
                new HardwareExecutionContext
                {
                    ProxyGenerationConfiguration = configuration,
                    HardwareRepresentation = hardwareRepresentation,
                });

        if (configuration.VerifyHardwareResults) OnVerifying(softMemory, memory);

        MemberExecutedOnHardware?.Invoke(this, new ServiceEventArgs<IMemberHardwareExecutionContext>(invocationContext));
    }

    private static void OnVerifying(SimpleMemory softMemory, SimpleMemory memory)
    {
        if (softMemory == null)
        {
            throw new ArgumentNullException(nameof(softMemory));
        }

        var mismatches = new List<HardwareExecutionResultMismatchException.Mismatch>();

        if (memory.CellCount != softMemory.CellCount)
        {
            int overflowIndex = Math.Min(memory.CellCount, softMemory.CellCount);
            mismatches.Add(new HardwareExecutionResultMismatchException.LengthMismatch(
                memory.CellCount,
                softMemory.CellCount,
                overflowIndex,
                memory.CellCount > softMemory.CellCount ? memory.Read4Bytes(overflowIndex) : Array.Empty<byte>(),
                softMemory.CellCount > memory.CellCount
                    ? softMemory.Read4Bytes(overflowIndex)
                    : Array.Empty<byte>()));
        }
        else
        {
            var hardBytes = new SimpleMemoryAccessor(memory).Get();
            var softBytes = new SimpleMemoryAccessor(softMemory).Get();

            for (var index = 0; index < hardBytes.Length; index += 4)
            {
                if (CellEquals(hardBytes, softBytes, index)) continue;
                mismatches.Add(new HardwareExecutionResultMismatchException.Mismatch(
                    index,
                    hardBytes[index..SimpleMemory.MemoryCellSizeBytes].ToArray(),
                    softBytes[index..SimpleMemory.MemoryCellSizeBytes].ToArray()));
            }
        }

        if (mismatches.Any())
        {
            throw new HardwareExecutionResultMismatchException(mismatches, memory.CellCount);
        }
    }

    private static bool CellEquals(Memory<byte> thisMemory, Memory<byte> thatMemory, int index)
    {
        var thisCell = thisMemory.Slice(index, SimpleMemory.MemoryCellSizeBytes).Span;
        var thatCell = thatMemory.Slice(index, SimpleMemory.MemoryCellSizeBytes).Span;

        for (int offset = 0; offset < SimpleMemory.MemoryCellSizeBytes; offset++)
        {
            if (thisCell[offset] != thatCell[offset]) return false;
        }

        return true;
    }

    private static Task OnCancelledAsync(
        IInvocation invocation,
        MemberInvocationContext invocationContext,
        MethodAsynchronicity methodAsynchronicity)
    {
        var softwareExecutionStopwatch = Stopwatch.StartNew();
        invocation.Proceed();
        softwareExecutionStopwatch.Stop();
        invocationContext.SoftwareExecutionInformation = new SoftwareExecutionInformation
        {
            SoftwareExecutionTimeMilliseconds = softwareExecutionStopwatch.ElapsedMilliseconds,
        };

        return methodAsynchronicity == MethodAsynchronicity.AsyncAction
            ? (Task)invocation.ReturnValue
            : Task.CompletedTask;
    }

    // Code taken from http://stackoverflow.com/a/28374134/220230
    private static MethodAsynchronicity GetMethodAsynchronicity(IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;

        if (returnType == typeof(Task)) return MethodAsynchronicity.AsyncAction;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return MethodAsynchronicity.AsyncFunction;
        }

        return MethodAsynchronicity.Synchronous;
    }

    private enum MethodAsynchronicity
    {
        Synchronous,
        AsyncAction,
        AsyncFunction,
    }

    private sealed class HardwareExecutionContext : IHardwareExecutionContext
    {
        public IProxyGenerationConfiguration ProxyGenerationConfiguration { get; set; }
        public IHardwareRepresentation HardwareRepresentation { get; set; }
    }

    private sealed class SoftwareExecutionInformation : ISoftwareExecutionInformation
    {
        public decimal SoftwareExecutionTimeMilliseconds { get; set; }
    }
}
