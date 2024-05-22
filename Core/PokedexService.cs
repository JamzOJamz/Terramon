using System.Collections.Generic;
using ReLogic.Reflection;

namespace Terramon.Core;

/// <summary>
///     Service class for managing the Pokédex functionality.
/// </summary>
public class PokedexService
{
    public readonly Dictionary<int, PokedexEntryStatus> Entries = new();

    public PokedexService()
    {
        if (Terramon.DatabaseV2 == null) return;
        foreach (var id in Terramon.DatabaseV2.Pokemon.Keys)
        {
            if (id > Terramon.MaxPokemonID) break;
            Entries.Add(id, PokedexEntryStatus.Undiscovered);
        }
    }
}

public enum PokedexEntryStatus : byte
{
    Undiscovered = 0,
    Seen = 1,
    Registered = 2
}