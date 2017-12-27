using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Hast.Common.Extensibility.Pipeline;
using Hast.Common.Extensions;
using Hast.Communication.Exceptions;
using Hast.Communication.Extensibility.Events;
using Hast.Communication.Extensibility.Pipeline;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Orchard;

namespace Hast.Communication
{
    public class MemberInvocationHandlerFactory : IMemberInvocationHandlerFactory
    {
        private readonly IWorkContextAccessor _wca;


        public MemberInvocationHandlerFactory(IWorkContextAccessor wca)
        {
            _wca = wca;
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
                        using (var workContext = _wca.CreateWorkContextScope())
                        {
                            // Although it says Method it can also be a property.
                            var memberFullName = invocation.Method.GetFullName();

                            var invocationContext = new MemberInvocationContext
                            {
                                Invocation = invocation,
                                MemberFullName = memberFullName,
                                HardwareRepresentation = hardwareRepresentation
                            };

                            var eventHandler = workContext.Resolve<IMemberInvocationEventHandler>();
                            eventHandler.MemberInvoking(invocationContext);

                            workContext.Resolve<IEnumerable<IMemberInvocationPipelineStep>>().InvokePipelineSteps(step =>
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
                                invocation.Proceed();

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
                                var memoryByteCount = memory.CellCount * SimpleMemory.MemoryCellSizeBytes;
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
                                    memory.Memory.CopyTo(softMemory.Memory, 0);

                                    var memoryArgumentIndex = invocation.Arguments
                                        .Select((argument, index) => new { Argument = argument, Index = index })
                                        .Single(argument => argument.Argument is SimpleMemory)
                                        .Index;
                                    invocation.SetArgumentValue(memoryArgumentIndex, softMemory);

                                    // This needs to happen before the awaited Execute() call below, otherwise the Task
                                    // in ReturnValue wouldn't be the original one any more.
                                    invocation.Proceed();

                                    if (methodAsynchronicity == MethodAsynchronicity.AsyncAction)
                                    {
                                        await (Task)invocation.ReturnValue;
                                    }
                                }

                                // At this point we checked that the hardware entry point does have a mapping.
                                var memberId = hardwareRepresentation
                                    .HardwareDescription
                                    .HardwareEntryPointNamesToMemberIdMappings[memberFullName];
                                invocationContext.ExecutionInformation = await workContext
                                    .Resolve<ICommunicationServiceSelector>()
                                    .GetCommunicationService(communicationChannelName)
                                    .Execute(
                                        memory,
                                        memberId,
                                        new HardwareExecutionContext { ProxyGenerationConfiguration = configuration, HardwareRepresentation = hardwareRepresentation });

                                if (configuration.VerifyHardwareResults)
                                {
                                    var mismatches = new List<HardwareExecutionResultMismatchException.Mismatch>();
                                    for (int i = 0; i < memory.CellCount; i++)
                                    {
                                        var hardwareBytes = memory.Read4Bytes(i);
                                        var softwareBytes = softMemory.Read4Bytes(i);
                                        if (!hardwareBytes.SequenceEqual(softwareBytes))
                                        {
                                            mismatches.Add(new HardwareExecutionResultMismatchException.Mismatch(
                                                i, hardwareBytes, softwareBytes));
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

                            eventHandler.MemberExecutedOnHardware(invocationContext);
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
            public IHardwareExecutionInformation ExecutionInformation { get; set; }
        }

        private class HardwareExecutionContext : IHardwareExecutionContext
        {
            public IProxyGenerationConfiguration ProxyGenerationConfiguration { get; set; }
            public IHardwareRepresentation HardwareRepresentation { get; set; }
        }
    }
}
