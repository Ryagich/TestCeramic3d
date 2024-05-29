using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


public class WhiteListPropertiesResolver : DefaultContractResolver
{
    private readonly HashSet<string> keepProps;

    public WhiteListPropertiesResolver(IEnumerable<string> propNamesToKeep)
    {
        keepProps = new HashSet<string>(propNamesToKeep);
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        if (!keepProps.Contains(property.PropertyName))
        {
            property.ShouldSerialize = _ => false;
        }
        return property;
    }
}