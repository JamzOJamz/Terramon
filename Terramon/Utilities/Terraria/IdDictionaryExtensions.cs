using ReLogic.Reflection;

namespace Terramon.Utilities.Terraria;

/// <summary>
///     Extension methods for Terraria's <see cref="IdDictionary" /> class.
/// </summary>
public static class IdDictionaryExtensions
{
    public static ushort Terramon(this IdDictionary search, string name)
        => (ushort)search.GetId($"{nameof(Terramon)}/{name}");
}