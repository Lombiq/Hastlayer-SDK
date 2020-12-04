using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration
{
    // Source: https://stackoverflow.com/a/55050425
    public static class ConfigurationExtensions
    {

        /// <summary>
        /// Serializes the configuration into JSON.
        /// </summary>
        public static string Serialize(this IConfiguration config) =>
            JsonConvert.SerializeObject(BindToExpandoObject(config));

        private static ExpandoObject BindToExpandoObject(IConfiguration config)
        {
            var result = new ExpandoObject();

            // retrieve all keys from your settings
            var configs = config.AsEnumerable();
            foreach (var kvp in configs)
            {
                var parent = (IDictionary<string, object>)result;
                var path = kvp.Key.Split(':');

                // create or retrieve the hierarchy (keep last path item for later)
                int lastIndex = 0;
                for (var i = 0; i < path.Length - 1; lastIndex = ++i)
                {
                    if (!parent.ContainsKey(path[i]))
                    {
                        parent.Add(path[i], new ExpandoObject());
                    }

                    parent = parent[path[i]] as IDictionary<string, object>;
                }

                if (kvp.Value == null)
                    continue;

                // add the value to the parent
                // note: in case of an array, key will be an integer and will be dealt with later
                var key = path[lastIndex];
                parent.Add(key, kvp.Value);
            }

            // at this stage, all arrays are seen as dictionaries with integer keys
            ReplaceWithArray(null, null, result);

            return result;
        }

        private static void ReplaceWithArray(ExpandoObject parent, string key, ExpandoObject input)
        {
            if (input == null)
                return;

            var dict = input as IDictionary<string, object>;
            var keys = dict.Keys.ToArray();

            // it's an array if all keys are integers
            if (keys.All(k => int.TryParse(k, out var dummy)))
            {
                var array = new object[keys.Length];
                foreach (var kvp in dict)
                {
                    array[int.Parse(kvp.Key, CultureInfo.InvariantCulture)] = kvp.Value;
                }

                var parentDict = parent as IDictionary<string, object>;
                parentDict.Remove(key);
                parentDict.Add(key, array);
            }
            else
            {
                foreach (var childKey in dict.Keys.ToList())
                {
                    ReplaceWithArray(input, childKey, dict[childKey] as ExpandoObject);
                }
            }
        }

    }
}
