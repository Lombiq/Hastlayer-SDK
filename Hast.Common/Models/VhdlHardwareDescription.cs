using Hast.Common.ContractResolvers;
using Hast.Layer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Hast.Common.Models;

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

        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(JsonConvert.SerializeObject(
            this,
            Formatting.None,
            GetJsonSerializerSettings()));
    }

    [SuppressMessage(
        "Security",
        "CA2327: Do not use insecure JsonSerializerSettings.",
        Justification = "See " + nameof(GetJsonSerializerSettings) + ".")]
    public static async Task<VhdlHardwareDescription> DeserializeAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return JsonConvert.DeserializeObject<VhdlHardwareDescription>(
                await reader.ReadToEndAsync(),
                GetJsonSerializerSettings());
    }

    [SuppressMessage(
        "Security",
        "CA2326:Do not use TypeNameHandling values other than None",
        Justification = "Not a concern, this is for internal caching.")]
    [SuppressMessage(
        "Security",
        "CA2327: Do not use insecure JsonSerializerSettings.",
        Justification = "Same.")]
    [SuppressMessage(
        "Security",
        "SCS0028:TypeNameHandling is set to the other value than 'None'. It may lead to deserialization vulnerability.",
        Justification = "Same.")]
    private static JsonSerializerSettings GetJsonSerializerSettings() =>
        new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new PrivateSetterContractResolver(),
        };
}
