using System.Reflection;
using System.Text;

namespace Terramon.Core.Helpers;

public static class PrettyPrint
{
    public static string Format<T>(T obj)
    {
        var type = typeof(T);
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var sb = new StringBuilder();
        sb.Append(type.Name + ' ');
        sb.Append("{ ");

        foreach (var field in fields)
        {
            var value = field.GetValue(obj);
            var formattedValue = value is string ? $"\"{value}\"" : value?.ToString();
            sb.Append($"{field.Name}: {formattedValue}, ");
        }

        if (sb.Length > 1) sb.Length -= 2;

        sb.Append(" }");

        return sb.ToString();
    }
}