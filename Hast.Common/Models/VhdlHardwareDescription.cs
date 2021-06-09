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

        public string TransformationId { get; set; }

        [JsonProperty]
        public string Language => LanguageName;

        public string VhdlSource { get; set; }

        public string XdcSource { get; set; }

        public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; set; }

        public IEnumerable<ITransformationWarning> Warnings { get; set; }

        public Task SerializeAsync(Stream stream)
        {
            if (string.IsNullOrEmpty(VhdlSource)) throw new InvalidOperationException("There is no VHDL source set.");

            using var writer = new StreamWriter(stream);
            return writer.WriteAsync(JsonConvert.SerializeObject(
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
