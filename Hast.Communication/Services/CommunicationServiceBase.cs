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
        protected  ILogger Logger { get; }

        abstract public string ChannelName { get; }
        public TextWriter TesterOutput { get; set; }


        protected CommunicationServiceBase(ILogger logger) => Logger = logger;


        abstract public Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext);


        protected CommunicationStateContext BeginExecution()
        {
            return new CommunicationStateContext
            {
                Stopwatch = Stopwatch.StartNew(),
                HardwareExecutionInformation = new HardwareExecutionInformation()
            };
        }

        protected void EndExecution(CommunicationStateContext context)
        {
            context.Stopwatch.Stop();

            context.HardwareExecutionInformation.FullExecutionTimeMilliseconds = context.Stopwatch.ElapsedMilliseconds;

            Logger.LogInformation("Full execution time: {0}ms", context.Stopwatch.ElapsedMilliseconds);
        }

        protected void SetHardwareExecutionTime(
            CommunicationStateContext context,
            IHardwareExecutionContext executionContext,
            ulong executionTimeClockCycles)
        {
            context.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds =
                1M / executionContext.HardwareRepresentation.DeviceManifest.ClockFrequencyHz * 1000 * executionTimeClockCycles;

            Logger.LogInformation("Hardware execution took " + context.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds + "ms.");
        }


        protected class CommunicationStateContext
        {
            public Stopwatch Stopwatch { get; set; }

            public HardwareExecutionInformation HardwareExecutionInformation { get; set; }
        }
    }
}
