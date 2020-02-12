using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public class JsonConverter : IJsonConverter
    {
        public string Serialize(object source) =>
            Newtonsoft.Json.JsonConvert.SerializeObject(source);
    }
}
