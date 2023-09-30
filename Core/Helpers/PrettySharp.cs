using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terramon.Core.Helpers;

public static class PrettySharp
{
    public static string Print<T>(T obj, int maxDepth = 3)
    {
        return FormatObject(obj, 0, maxDepth);
    }

    private static string FormatObject(object obj, int currentDepth, int maxDepth)
    {
        var type = obj.GetType();
        if (currentDepth >= maxDepth) return $"[{type.Name}]";

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var sb = new StringBuilder();
        sb.Append(type.Name + ' ');
        sb.Append("{ ");

        foreach (var field in fields)
        {
            var value = field.GetValue(obj);
            Main.NewText(IsSimpleType(field.FieldType));
            var formattedValue = value is string ? $"\"{value}\"" :
                IsSimpleType(field.FieldType) ? value?.ToString() : FormatObject(value, currentDepth + 1, maxDepth);
            sb.Append($"{field.Name}: {formattedValue}, ");
        }

        if (fields.Length > 0) sb.Length -= 2;

        sb.Append(" }");

        return sb.ToString();
    }

    private static bool IsSimpleType(Type type)
    {
        return
            type.IsPrimitive ||
            new[]
            {
                typeof(string),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid)
            }.Any(t => t == type) ||
            type.IsEnum ||
            Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             IsSimpleType(type.GetGenericArguments()[0])) ||
            type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
    }
}