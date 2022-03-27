using Newtonsoft.Json;

namespace Hast.Common.Services;

public class DefaultJsonConverter : IJsonConverter
{
    public string Serialize(object source) => JsonConvert.SerializeObject(source);

    public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
}
