using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Synthesis.Abstractions.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Hast.Xilinx.Abstractions.Helpers;

public static class MemoryConfigurationHelper
{
    public static void ConfigureMemoryForVitis(MemoryConfiguration memory, IHardwareGenerationConfiguration hardwareGeneration)
    {
        memory.Alignment = 4096;
        memory.MinimumPrefix = 4;

        if (!hardwareGeneration.CustomConfiguration.TryGetValue("OpenClConfiguration", out var value)) return;
        var custom = (value is JObject jObject ? jObject : JObject.FromObject(value))
            .Properties()
            .ToDictionary(x => x.Name, x => x.Value);
        if (custom.TryGetValue(nameof(memory.Alignment), out var alignment)) memory.Alignment = alignment.Value<int>();
        if (custom.TryGetValue(nameof(memory.MinimumPrefix), out var prefix)) memory.MinimumPrefix = prefix.Value<int>();
    }
}
