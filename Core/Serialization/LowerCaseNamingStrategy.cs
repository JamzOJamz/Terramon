using Newtonsoft.Json.Serialization;

namespace Terramon.Core.Serialization;

public class LowerCaseNamingStrategy : NamingStrategy
{
    protected override string ResolvePropertyName(string name)
    {
        return name.ToLowerInvariant();
    }
}