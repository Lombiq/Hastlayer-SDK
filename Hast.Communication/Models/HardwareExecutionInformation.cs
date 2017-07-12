using System;
using Hast.Layer;

namespace Hast.Communication.Models
{
    public class HardwareExecutionInformation : IHardwareExecutionInformation
    {
        public decimal HardwareExecutionTimeMilliseconds { get; set; }
        public decimal FullExecutionTimeMilliseconds { get; set; }
        public DateTime StartedUtc { get; set; }


        public HardwareExecutionInformation()
        {
            StartedUtc = DateTime.UtcNow;
            HardwareExecutionTimeMilliseconds = 0;
            FullExecutionTimeMilliseconds = 0;
        }
    }
}
