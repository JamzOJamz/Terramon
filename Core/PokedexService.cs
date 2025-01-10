namespace Terramon.Core;

/// <summary>
///     Class to hold the status of a Pokédex entry and the name of the player who last updated it.
/// </summary>
public class PokedexEntry(PokedexEntryStatus status, string lastUpdatedBy = null)
{
    public PokedexEntryStatus Status { get; set; } = status;
    public string LastUpdatedBy { get; set; } = lastUpdatedBy;
    public bool Unlisted { get; init; } // Whether this entry should be hidden from the Pokédex.
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
            if (id > Terramon.LoadedPokemonCount) break;
            Entries.Add(id, new PokedexEntry(PokedexEntryStatus.Undiscovered));
        }
    }

    /// <summary>
    ///     The amount of Pokémon that have been registered in this Pokédex.
    /// </summary>
    public int RegisteredCount =>
        Entries.Count(e => !e.Value.Unlisted && e.Value.Status == PokedexEntryStatus.Registered);

    /// <summary>
    ///     The amount of Pokémon that have been seen in this Pokédex.
    /// </summary>
    public int SeenCount => Entries.Count(e => !e.Value.Unlisted && e.Value.Status == PokedexEntryStatus.Seen);

    /// <summary>
    ///     The amount of Pokémon that have not yet been seen or registered in this Pokédex.
    /// </summary>
    public int UndiscoveredCount =>
        Entries.Count(e => !e.Value.Unlisted && e.Value.Status == PokedexEntryStatus.Undiscovered);

    public List<int[]> GetEntriesForSaving()
    {
        return Entries
            .Where(entry => entry.Value.Status != PokedexEntryStatus.Undiscovered)
            .Select(entry => new[] { entry.Key, (byte)entry.Value.Status })
            .ToList();
    }

    public void LoadEntries(IEnumerable<int[]> entries)
    {
        foreach (var entryData in entries)
            // If the entry is not in the database, force create it (backwards compatibility so saves don't get overwritten)
            if (!Entries.ContainsKey(entryData[0]))
            {
                Entries.Add(entryData[0], new PokedexEntry((PokedexEntryStatus)entryData[1]) { Unlisted = true });
            }
            else
            {
                var id = entryData[0];
                var status = (PokedexEntryStatus)entryData[1];
                Entries[id] = new PokedexEntry(status);
            }
    }
}

public enum PokedexEntryStatus : byte
{
    Undiscovered,
    Seen,
    Registered
}