using Newtonsoft.Json.Linq;

namespace System.Collections.Generic;

public static class DictionaryExtensions
{
    public static T GetOrAddCustomConfiguration<T>(this IDictionary<string, object> customConfiguration, string key)
        where T : new()
    {
        if (customConfiguration.TryGetValue(key, out var config))
        {
            if (config is T typedConfig) return typedConfig;
            if (config is JToken jToken) return jToken.ToObject<T>();
        }

        var newInstance = new T();
        customConfiguration[key] = newInstance;
        return newInstance;
    }

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
