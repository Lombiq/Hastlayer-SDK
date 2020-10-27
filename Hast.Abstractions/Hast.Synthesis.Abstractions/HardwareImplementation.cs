using Hast.Layer;
using System.Collections.Generic;

namespace Hast.Synthesis.Abstractions
{
    public class HardwareImplementation : IHardwareImplementation
    {
        public string BinaryPath { get; set; }
        public Dictionary<string, decimal> ResourceUsagePercent { get; } = new Dictionary<string, decimal>();
        public decimal PowerUsageWatts { get; set; }
    }
}
