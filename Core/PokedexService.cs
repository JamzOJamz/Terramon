using System.Collections.Generic;
using ReLogic.Reflection;

namespace Terramon.Core;

public class PokedexService
{
    public readonly Dictionary<ushort, byte> Entries = new();

    public PokedexService()
    {
        if (Terramon.Database == null) return;
        foreach (var id in Terramon.Database.Pokemon.Keys)
        {
            if (id > Terramon.MaxPokemonID) break;
            Entries.Add(id, PokedexEntryStatus.Undiscovered);
        }
    }
}

public static class PokedexEntryStatus
{
    public const byte Undiscovered = 0;
    public const byte Seen = 1;
    public const byte Registered = 2;
    public static readonly IdDictionary Search = IdDictionary.Create(typeof(PokedexEntryStatus), typeof(byte));
}