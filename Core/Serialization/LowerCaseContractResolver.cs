using Newtonsoft.Json.Serialization;

namespace Terramon.Core.Serialization;

public class LowerCaseContractResolver : DefaultContractResolver
{
    protected override string ResolvePropertyName(string propertyName)
    {
        return propertyName.ToLower();
    }
}