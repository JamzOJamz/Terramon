using System.Collections.Generic;

namespace Terramon.Core;

/// <summary>
///     Class to hold the status of a Pokédex entry and the name of the player who last updated it.
/// </summary>
public class PokedexEntry(PokedexEntryStatus status, string lastUpdatedBy = null)
{
    public PokedexEntryStatus Status { get; set; } = status;
    public string LastUpdatedBy { get; set; } = lastUpdatedBy;
}

/// <summary>
///     Service class for managing the Pokédex functionality.
/// </summary>
public class PokedexService
{
    public readonly Dictionary<int, PokedexEntry> Entries = new();

    public PokedexService()
    {
        if (Terramon.DatabaseV2 == null) return;
        foreach (var id in Terramon.DatabaseV2.Pokemon.Keys)
        {
            if (id > Terramon.MaxPokemonID) break;
            Entries.Add(id, new PokedexEntry(PokedexEntryStatus.Undiscovered));
        }
    }
}

public enum PokedexEntryStatus : byte
{
    Undiscovered = 0,
    Seen = 1,
    Registered = 2
}