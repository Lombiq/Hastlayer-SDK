using Hast.Common.ContractResolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static T GetOrAddCustomConfiguration<T>(this IDictionary<string, object> customConfiguration, string key)
            where T : new()
        {

            if (customConfiguration.TryGetValue(key, out var config))
            {
                // If this is a remote transformation then custom configs won't necessarily be properly deserialized at
                // this point, so need to handle them specifically.
                if (config is JObject jObject)
                {
                    var deserialized = jObject
                        .ToObject<T>(new JsonSerializer { ContractResolver = new PrivateSetterContractResolver() });
                    customConfiguration[key] = deserialized;
                    return deserialized;
                }

                return (T)config;
            }

            return (T)(customConfiguration[key] = new T());
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}
