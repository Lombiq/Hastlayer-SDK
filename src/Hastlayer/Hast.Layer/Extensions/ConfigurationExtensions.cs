using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration;

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
        foreach (var pair in configs)
        {
            var parent = (IDictionary<string, object>)result;
            var path = pair.Key.Split(':');

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

            if (pair.Value == null) continue;

            // add the value to the parent
            // note: in case of an array, key will be an integer and will be dealt with later
            var key = path[lastIndex];
            parent.Add(key, pair.Value);
        }

        // at this stage, all arrays are seen as dictionaries with integer keys
        ReplaceWithArray(parent: null, keyInParent: null, result);

        return result;
    }

    private static void ReplaceWithArray(ExpandoObject parent, string keyInParent, ExpandoObject input)
    {
        if (input == null) return;

        var inputDictionary = (IDictionary<string, object>)input;
        var keys = inputDictionary.Keys.ToArray();

        // It's an array if all keys are integers.
        if (keys.TrueForAll(key => int.TryParse(key, out var index) && index >= 0 && index < keys.Length))
        {
            var array = new object[keys.Length];
            foreach (var (key, value) in inputDictionary)
            {
                array[key.ToTechnicalInt()] = value;
            }

            var parentDictionary = (IDictionary<string, object>)parent;
            parentDictionary.Remove(keyInParent);
            parentDictionary.Add(keyInParent, array);
        }
        else
        {
            foreach (var childKey in inputDictionary.Keys.ToList())
            {
                ReplaceWithArray(input, childKey, inputDictionary[childKey] as ExpandoObject);
            }
        }
    }
}
