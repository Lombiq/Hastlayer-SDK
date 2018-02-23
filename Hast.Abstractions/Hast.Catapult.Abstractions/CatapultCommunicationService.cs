using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Catapult.Abstractions
{
    public class CatapultCommunicationService : CommunicationServiceBase
    {
        public override string ChannelName => CatapultManifestProvider.DeviceName;


        public override Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            throw new NotImplementedException();

            //var context = BeginExecution();


            //EndExecution(context);

            //return context.HardwareExecutionInformation;
        }
    }
}
