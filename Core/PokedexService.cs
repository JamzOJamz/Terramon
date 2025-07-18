namespace Terramon.Core;

/// <summary>
///     Class to hold the status of a Pokédex entry and the name of the player who last updated it.
/// </summary>
public class PokedexEntry(PokedexEntryStatus status, string lastUpdatedBy = null)
{
    public PokedexEntryStatus Status { get; set; } = status;
    public string LastUpdatedBy { get; set; } = lastUpdatedBy;
    public bool Unlisted { get; init; } // Whether this entry should be hidden from the Pokédex.
    public int CaughtCount { get; set; } // Number of this Pokémon caught.
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
        
        foreach (var id in Terramon.DatabaseV2.Pokemon.Keys.Take(Terramon.LoadedPokemonCount))
            Entries.Add(id, new PokedexEntry(PokedexEntryStatus.Undiscovered));
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

    /// <summary>
    ///     The total number of Pokémon caught across all species.
    /// </summary>
    public int TotalCaughtCount =>
        Entries.Where(e => !e.Value.Unlisted).Sum(e => e.Value.CaughtCount);

    public List<int[]> GetEntriesForSaving()
    {
        return Entries
            .Where(entry => entry.Value.Status != PokedexEntryStatus.Undiscovered || entry.Value.CaughtCount > 0)
            .Select(entry => new[] { entry.Key, (int)entry.Value.Status, entry.Value.CaughtCount })
            .ToList();
    }

    public void LoadEntries(IEnumerable<int[]> entries)
    {
        foreach (var entryData in entries)
        {
            var id = entryData[0];
            var status = (PokedexEntryStatus)entryData[1];
            
            // Backwards compatibility: old saves only have 2 elements (id, status)
            var caughtCount = entryData.Length > 2 ? entryData[2] :
                status == PokedexEntryStatus.Registered ? 1 : 0;

            // If the entry is not in the database, force create it (backwards compatibility so saves don't get overwritten)
            if (!Entries.TryGetValue(id, out var entry))
            {
                Entries.Add(id, new PokedexEntry(status) { Unlisted = true, CaughtCount = caughtCount });
            }
            else
            {
                entry.Status = status;
                entry.CaughtCount = caughtCount;
            }
        }
    }
}

public enum PokedexEntryStatus : byte
{
    Undiscovered,
    Seen,
    Registered
}