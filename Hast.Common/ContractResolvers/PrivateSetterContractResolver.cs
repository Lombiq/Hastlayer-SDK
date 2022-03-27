using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hast.Common.ContractResolvers;

public class PrivateSetterContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var jProperty = base.CreateProperty(member, memberSerialization);
        if (jProperty.Writable)
            return jProperty;

        jProperty.Writable = member.IsPropertyWithSetter();

        return jProperty;
    }
}

public class PrivateSetterCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var jProperty = base.CreateProperty(member, memberSerialization);
        if (jProperty.Writable)
            return jProperty;

        jProperty.Writable = member.IsPropertyWithSetter();

        return jProperty;
    }
}

internal static class MemberInfoExtensions
{
    internal static bool IsPropertyWithSetter(this MemberInfo member)
    {
        var property = member as PropertyInfo;

        return property?.GetSetMethod(nonPublic: true) != null;
    }
}
