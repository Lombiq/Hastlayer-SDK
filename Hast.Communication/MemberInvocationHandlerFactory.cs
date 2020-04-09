using Castle.DynamicProxy;
using Hast.Common.Extensibility.Pipeline;
using Hast.Common.Extensions;
using Hast.Common.Interfaces;
using Hast.Communication.Exceptions;
using Hast.Communication.Extensibility;
using Hast.Communication.Extensibility.Events;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Communication
{
    public class MemberInvocationHandlerFactory : IMemberInvocationHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;


        public MemberInvocationHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public MemberInvocationHandler CreateMemberInvocationHandler(
            IHardwareRepresentation hardwareRepresentation,
            object target,
            IProxyGenerationConfiguration configuration)
        {
            return invocation =>
                {
                    var methodAsynchronicity = GetMethodAsynchronicity(invocation);

                    if (methodAsynchronicity == MethodAsynchronicity.AsyncFunction)
                    {
                        throw new NotSupportedException("Only async methods that return a Task, not Task<T>, are supported.");
                    }


                    async Task invocationHandler()
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            // Although it says Method it can also be a property.
                            var memberFullName = invocation.Method.GetFullName();

                            var invocationContext = new MemberInvocationContext
                            {
                                Invocation = invocation,
                                MemberFullName = memberFullName,
                                HardwareRepresentation = hardwareRepresentation
                            };

                            InvokeEventHandlers<IMemberInvocationPipelineStepContext>(scope.ServiceProvider, invocationContext);

                            scope.ServiceProvider.GetService<IEnumerable<IMemberInvocationPipelineStep>>().InvokePipelineSteps(step =>
                                {
                                    invocationContext.HardwareExecutionIsCancelled = step.CanContinueHardwareExecution(invocationContext);
                                });

                            if (!invocationContext.HardwareExecutionIsCancelled)
                            {
                                var hardwareMembers = hardwareRepresentation.HardwareDescription.HardwareEntryPointNamesToMemberIdMappings;
                                var memberNameAlternates = new HashSet<string>(hardwareMembers.Keys.SelectMany(member => member.GetMemberNameAlternates()));
                                if (!hardwareMembers.ContainsKey(memberFullName) && !memberNameAlternates.Contains(memberFullName))
                                {
                                    invocationContext.HardwareExecutionIsCancelled = true;
                                }
                            }

                            if (invocationContext.HardwareExecutionIsCancelled)
                            {
                                var softwareExecutionStopwatch = Stopwatch.StartNew();
                                invocation.Proceed();
                                softwareExecutionStopwatch.Stop();
                                invocationContext.SoftwareExecutionInformation = new SoftwareExecutionInformation
                                {
                                    SoftwareExecutionTimeMilliseconds = softwareExecutionStopwatch.ElapsedMilliseconds
                                };

                                if (methodAsynchronicity == MethodAsynchronicity.AsyncAction)
                                {
                                    await (Task)invocation.ReturnValue;
                                }

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

                            var memory = (SimpleMemory)invocation.Arguments.SingleOrDefault(argument => argument is SimpleMemory);
                            if (memory != null)
                            {
                                var memoryByteCount = (ulong)memory.CellCount * SimpleMemory.MemoryCellSizeBytes;
                                if (memoryByteCount > deviceManifest.AvailableMemoryBytes)
                                {
                                    throw new InvalidOperationException(
                                        "The input is too large to fit into the device's memory: the input is " +
                                        memoryByteCount + " bytes, the available memory is " +
                                        deviceManifest.AvailableMemoryBytes + " bytes.");
                                }

                                SimpleMemory softMemory = null;

                                if (configuration.VerifyHardwareResults)
                                {
                                    softMemory = new SimpleMemory(memory.CellCount);
                                    var memoryBytes = new SimpleMemoryAccessor(memory).Get();
                                    memoryBytes.CopyTo(new SimpleMemoryAccessor(softMemory).Get());

                                    var memoryArgumentIndex = invocation.Arguments
                                        .Select((argument, index) => new { Argument = argument, Index = index })
                                        .Single(argument => argument.Argument is SimpleMemory)
                                        .Index;
                                    invocation.SetArgumentValue(memoryArgumentIndex, softMemory);

                                    var softwareExecutionStopwatch = Stopwatch.StartNew();
                                    // This needs to happen before the awaited Execute() call below, otherwise the Task
                                    // in ReturnValue wouldn't be the original one any more.
                                    invocation.Proceed();
                                    softwareExecutionStopwatch.Stop();
                                    invocationContext.SoftwareExecutionInformation = new SoftwareExecutionInformation
                                    {
                                        SoftwareExecutionTimeMilliseconds = softwareExecutionStopwatch.ElapsedMilliseconds
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
                                invocationContext.HardwareExecutionInformation = await scope
                                    .ServiceProvider
                                    .GetService<ICommunicationServiceSelector>()
                                    .GetCommunicationService(communicationChannelName)
                                    .Execute(
                                        memory,
                                        memberId,
                                        new HardwareExecutionContext { ProxyGenerationConfiguration = configuration, HardwareRepresentation = hardwareRepresentation });

                                if (configuration.VerifyHardwareResults)
                                {
                                    var mismatches = new List<HardwareExecutionResultMismatchException.Mismatch>();

                                    if (memory.CellCount != softMemory.CellCount)
                                    {
                                        int overflowIndex = Math.Min(memory.CellCount, softMemory.CellCount);
                                        mismatches.Add(new HardwareExecutionResultMismatchException.LengthMismatch(
                                            memory.CellCount,
                                            softMemory.CellCount,
                                            overflowIndex,
                                            memory.CellCount > softMemory.CellCount ? memory.Read4Bytes(overflowIndex) : new byte[0],
                                            softMemory.CellCount > memory.CellCount ? softMemory.Read4Bytes(overflowIndex) : new byte[0]));
                                    }
                                    else
                                    {
                                        for (int i = 0; i < memory.CellCount; i++)
                                        {
                                            if (!memory.Read4Bytes(i).SequenceEqual(softMemory.Read4Bytes(i)))
                                            {
                                                mismatches.Add(new HardwareExecutionResultMismatchException.Mismatch(
                                                    i, memory.Read4Bytes(i), softMemory.Read4Bytes(i)));
                                            }
                                        }
                                    }

                                    if (mismatches.Any())
                                    {
                                        throw new HardwareExecutionResultMismatchException(mismatches);
                                    }
                                }
                            }
                            else
                            {
                                throw new NotSupportedException(
                                    "Only SimpleMemory-using implementations are supported for hardware execution. The invocation didn't include a SimpleMemory argument.");
                            }

                            InvokeEventHandlers<IMemberHardwareExecutionContext>(scope.ServiceProvider, invocationContext);
                        }
                    }


                    if (methodAsynchronicity == MethodAsynchronicity.AsyncAction)
                    {
                        invocation.ReturnValue = invocationHandler();
                    }
                    else
                    {
                        invocationHandler().Wait();
                    }
                };
        }

        private void InvokeEventHandlers<T>(IServiceProvider provider, T context) where T : IMemberInvocationContext
        {
            var eventHandlers = provider.GetService<IEnumerable<EventHandler<T>>>();
            foreach (var eventHandler in eventHandlers) eventHandler?.Invoke(this, context);
        }

        // Code taken from http://stackoverflow.com/a/28374134/220230
        private static MethodAsynchronicity GetMethodAsynchronicity(IInvocation invocation)
        {
            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(Task)) return MethodAsynchronicity.AsyncAction;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return MethodAsynchronicity.AsyncFunction;

            return MethodAsynchronicity.Synchronous;
        }


        private enum MethodAsynchronicity
        {
            Synchronous,
            AsyncAction,
            AsyncFunction
        }

        private class MemberInvocationContext : IMemberInvocationPipelineStepContext, IMemberHardwareExecutionContext
        {
            public bool HardwareExecutionIsCancelled { get; set; }
            public IInvocation Invocation { get; set; }
            public string MemberFullName { get; set; }
            public IHardwareRepresentation HardwareRepresentation { get; set; }
            public IHardwareExecutionInformation HardwareExecutionInformation { get; set; }
            public ISoftwareExecutionInformation SoftwareExecutionInformation { get; set; }
        }

        private class HardwareExecutionContext : IHardwareExecutionContext
        {
            public IProxyGenerationConfiguration ProxyGenerationConfiguration { get; set; }
            public IHardwareRepresentation HardwareRepresentation { get; set; }
        }

        private class SoftwareExecutionInformation : ISoftwareExecutionInformation
        {
            public decimal SoftwareExecutionTimeMilliseconds { get; set; }
        }
    }
}
