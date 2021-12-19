using Hast.Common.ContractResolvers;
using Hast.Layer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Common.Models
{
    public class VhdlHardwareDescription : IHardwareDescription
    {
        public const string LanguageName = "VHDL";

        [JsonProperty]
        public string TransformationId { get; set; }

        [JsonProperty]
        public string Language => LanguageName;

        [JsonProperty]
        public string VhdlSource { get; set; }

        [JsonProperty]
        public string XdcSource { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; set; }

        [JsonProperty]
        public IEnumerable<ITransformationWarning> Warnings { get; set; }

        public async Task SerializeAsync(Stream stream)
        {
            if (string.IsNullOrEmpty(VhdlSource)) throw new InvalidOperationException("There is no VHDL source set.");

            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(JsonConvert.SerializeObject(
                this,
                Formatting.None,
                GetJsonSerializerSettings()));
        }

        public static async Task<VhdlHardwareDescription> DeserializeAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<VhdlHardwareDescription>(
                    await reader.ReadToEndAsync(),
                    GetJsonSerializerSettings());
        }

        private static JsonSerializerSettings GetJsonSerializerSettings() =>
            new()
            {
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new PrivateSetterContractResolver(),
            };
    }
}
