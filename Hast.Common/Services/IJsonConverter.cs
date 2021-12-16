using Hast.Common.Interfaces;

namespace Hast.Common.Services
{
    public interface IJsonConverter : ISingletonDependency
    {
        string Serialize(object source);
        T Deserialize<T>(string json);
    }
}
