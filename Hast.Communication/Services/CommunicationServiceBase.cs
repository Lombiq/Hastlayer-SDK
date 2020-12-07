using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Communication.Services
{
    public abstract class CommunicationServiceBase : ICommunicationService
    {
        protected ILogger Logger { get; }

        public abstract string ChannelName { get; }
        public TextWriter TesterOutput { get; set; }

        protected CommunicationServiceBase(ILogger logger) => Logger = logger;

        public abstract Task<IHardwareExecutionInformation> ExecuteAsync(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext);

        protected CommunicationStateContext BeginExecution() => new CommunicationStateContext
        {
            Stopwatch = Stopwatch.StartNew(),
            HardwareExecutionInformation = new HardwareExecutionInformation(),
        };

        protected void EndExecution(CommunicationStateContext context)
        {
            context.Stopwatch.Stop();

            context.HardwareExecutionInformation.FullExecutionTimeMilliseconds = context.Stopwatch.ElapsedMilliseconds;

            Logger.LogInformation("Full execution time: {0}ms", context.Stopwatch.ElapsedMilliseconds);
        }

        protected void SetHardwareExecutionTime(
            CommunicationStateContext context,
            IHardwareExecutionContext executionContext,
            ulong executionTimeClockCycles,
            uint? clockFrequency = null)
        {
            var frequency = clockFrequency ?? executionContext.HardwareRepresentation.DeviceManifest.ClockFrequencyHz;

            var milliseconds = 1M / frequency * 1_000 * executionTimeClockCycles;
            context.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds = milliseconds;

            Logger.LogInformation(
                "The {0} clock frequency is {1:0.###} MHz",
                clockFrequency == null ? "standard" : "specified",
                frequency / 1_000_000.0);

            Logger.LogInformation("Hardware execution took {0:0.0000}ms.", milliseconds);
        }

        protected class CommunicationStateContext
        {
            public Stopwatch Stopwatch { get; set; }

            public HardwareExecutionInformation HardwareExecutionInformation { get; set; }
        }
    }
}
