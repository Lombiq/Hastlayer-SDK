using Hast.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public interface IJsonConverter : ISingletonDependency
    {
        string Serialize(object source);
        T Deserialize<T>(string json);
    }
}
