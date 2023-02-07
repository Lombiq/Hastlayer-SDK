using Hast.VhdlBuilder.Representation.Expression;
using Newtonsoft.Json;

namespace Hast.VhdlBuilder;

/// <summary>
/// Populates Json.NET settings with VhdlBuilder-specific configuration to allow the de/serialization of VHDL objects.
/// </summary>
public static class JsonSerializerSettingsPopulator
{
    public static void PopulateSettings(JsonSerializerSettings settings) =>
        settings.Converters.Add(BinaryOperator.JsonConverter);
}
