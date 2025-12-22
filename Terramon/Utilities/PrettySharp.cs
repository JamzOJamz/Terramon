using System.Reflection;
using System.Text;

namespace Terramon.Utilities
{
    public static class PrettySharp
    {
        public static string Print<T>(T obj, int maxDepth = 3)
        {
            return FormatObject(obj, 0, maxDepth, []);
        }

        private static string FormatObject(object obj, int currentDepth, int maxDepth, HashSet<object> visited)
        {
            if (obj == null) return "null";
            if (visited.Contains(obj)) return "[Cyclic]";

            var type = obj.GetType();
            if (currentDepth >= maxDepth) return $"[{type.Name}]";

            if (IsSimpleType(type)) return obj.ToString() ?? "null";

            visited.Add(obj);

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var sb = new StringBuilder();
            sb.Append(type.Name + " { ");

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                var formattedValue = value is string ? $"\"{value}\"" :
                    IsSimpleType(field.FieldType) ? value?.ToString() :
                    FormatObject(value, currentDepth + 1, maxDepth, visited);
                if (value == null) formattedValue = "null";
                sb.Append($"{field.Name}: {formattedValue}, ");
            }

            if (fields.Length > 0) sb.Length -= 2; // remove trailing comma+space
            sb.Append(" }");

            visited.Remove(obj);
            return sb.ToString();
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid) ||
                   Convert.GetTypeCode(type) != TypeCode.Object ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    IsSimpleType(type.GetGenericArguments()[0])) ||
                   type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
        }
    }
}