using Hast.Samples.Consumer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer
{
    public class Gui
    {
        public Dictionary<string, ConsumerConfiguration> SavedConfigurations { get; }

        public Gui(Dictionary<string, ConsumerConfiguration> savedConfigurations) =>
            SavedConfigurations = savedConfigurations;

        public async Task<ConsumerConfiguration> BuildConfiguration() =>
            SavedConfigurations.Values.FirstOrDefault() ?? new();
    }
}
