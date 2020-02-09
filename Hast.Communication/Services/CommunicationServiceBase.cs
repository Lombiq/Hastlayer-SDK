using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Communication.Services
{
    public abstract class CommunicationServiceBase : ICommunicationService
    {
        public ILogger Logger { get; set; }

        abstract public string ChannelName { get; }

        private TextWriter _testerOutput;
        public TextWriter TesterOutput
        {
            get => _testerOutput;
            set => _testerOutput = value;
        }

        public CommunicationServiceBase()
        {
            Logger = NullLogger.Instance;
        }


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

            Logger.Information("Full execution time: {0}ms", context.Stopwatch.ElapsedMilliseconds);
        }

        protected void SetHardwareExecutionTime(
            CommunicationStateContext context,
            IHardwareExecutionContext executionContext, 
            ulong executionTimeClockCycles)
        {
            context.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds = 
                1M / executionContext.HardwareRepresentation.DeviceManifest.ClockFrequencyHz * 1000 * executionTimeClockCycles;

            Logger.Information("Hardware execution took " + context.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds + "ms.");
        }


        protected class CommunicationStateContext
        {
            public Stopwatch Stopwatch { get; set; }

            public HardwareExecutionInformation HardwareExecutionInformation { get; set; }
        }
    }
}
