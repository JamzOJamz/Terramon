using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Terramon.Core;
using Terramon.ID;

namespace Terramon.DataGen;

internal static class Program
{
    /// <summary>
    ///     Maximum Pokémon ID to fetch data for (not including <see cref="ExtraPokemonIDs" />).
    /// </summary>
    private const ushort MaxPokemonIDToFetch = 151;

    /// <summary>
    ///     Whether the program is running from /bin or launched directly.
    /// </summary>
    private static bool Exec;

    /// <summary>
    ///     Extra Pokémon IDs to fetch (like starters from later generations).
    /// </summary>
    private static readonly int[] ExtraPokemonIDs =
    [
        // Gen 2 starters
        // 152, 155, 158, // Chikorita, Cyndaquil, Totodile

        // Gen 3 starters  
        // 252, 255, 258, // Treecko, Torchic, Mudkip

        // Gen 4 starters
        // 387, 390, 393, // Turtwig, Chimchar, Piplup

        // Gen 5 starters
        // 495, 498, 501, // Snivy, Tepig, Oshawott

        // Gen 6 starters
        // 650, 653, 656, // Chespin, Fennekin, Froakie

        // Gen 7 starters
        // 722, 725, 728, // Rowlet, Litten, Popplio

        // Gen 8 starters
        // 810, 813, 816, // Grookey, Scorbunny, Sobble

        // Gen 9 starters
        // 906, 909, 912  // Sprigatito, Fuecoco, Quaxly
    ];

    private static readonly HttpClient HttpClient = new();
    private static readonly TextInfo InvariantTextInfo = CultureInfo.InvariantCulture.TextInfo;

    private static readonly Dictionary<string, string> IdentifierMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Medium", "MediumFast" },
    };

    private static async Task Main()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();
        var totalPokemonCount = MaxPokemonIDToFetch + ExtraPokemonIDs.Length;

        var dir = Path.GetFileName(Environment.CurrentDirectory);
        // Console.WriteLine($"dir: {dir}, asm: {assemblyName.Name}");

        Exec = !dir.Equals(assemblyName.Name, StringComparison.Ordinal);

        Console.WriteLine("========================================");
        Console.WriteLine($"Running {assemblyName.Name} v{assemblyName.Version}");
        Console.WriteLine($"Launched {(Exec ? $"from {assemblyName.Name}.exe" : "using dotnet run")}");
        Console.WriteLine("========================================\n");

        Console.WriteLine("WARNING: This program will overwrite existing PokemonDB*.json files in:");
        Console.WriteLine("Terramon/Assets/Data\n");
        Console.WriteLine($"It will generate data for {totalPokemonCount} Pokémon.\n");

        Console.WriteLine("Press any key to continue, or CTRL+C to quit...");
        Console.ReadKey(true);

        var startTime = DateTime.Now;

        Console.Clear();
        Console.WriteLine($"Process started at {startTime:T}\n");

        var pokemon = new Dictionary<ushort, DatabaseV2.PokemonSchema>();

        for (ushort id = 1; id <= MaxPokemonIDToFetch; id++)
        {
            try
            {
                Console.WriteLine($"Fetching Pokémon ID {id}...");
                var pokemonSchema = await FetchPokemonData(id);
                Console.WriteLine($"Fetched {pokemonSchema.Identifier} (ID {id}) successfully.\n");
                pokemon[id] = pokemonSchema;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Pokémon {id}: {ex.Message}");
            }
        }

        var databaseV2 = new DatabaseV2
        {
            Pokemon = new ReadOnlyDictionary<ushort, DatabaseV2.PokemonSchema>(pokemon)
        };

        var json = databaseV2.Serialize();
        var jsonMinified = databaseV2.Serialize(true);

        var outDir = Path.Combine(Environment.CurrentDirectory, "..");
        if (Exec)
            outDir = Path.Combine(outDir, "..", "..", "..");
        outDir = Path.Combine(outDir, "Terramon", "Assets", "Data");

        var outFile = Path.Combine(outDir, "PokemonDB.json");
        var outFileMinified = Path.Combine(outDir, "PokemonDB-min.json");

        Console.WriteLine($"Writing full JSON to {Path.GetFullPath(outFile)}...");
        await File.WriteAllTextAsync(outFile, json);

        Console.WriteLine($"Writing minified JSON to {Path.GetFullPath(outFileMinified)}...");
        await File.WriteAllTextAsync(outFileMinified, jsonMinified);

        var endTime = DateTime.Now;
        Console.WriteLine($"\nProcess completed at {endTime:T} (Duration: {endTime - startTime})");
    }

    private static string GetCacheDirectory(string? subdir = null)
    {
        var cacheDir = Environment.CurrentDirectory;
        if (Exec)
            cacheDir = Path.Combine(cacheDir, "..", "..", "..");
        cacheDir = Path.Combine(cacheDir, "Cache");
        if (subdir != null)
            cacheDir = Path.Combine(cacheDir, subdir);
        return cacheDir;
    }

    private static async Task<DatabaseV2.PokemonSchema> FetchPokemonData(int id)
    {
        // --- Handle caching in accordance to PokéAPI's fair use policy ---
        var pokeCacheDir = GetCacheDirectory("Pokemon");
        var pokeFile = Path.Combine(pokeCacheDir, $"{id}.pkmn");
        
        string? jsonContent;

        if (File.Exists(pokeFile))
        {
            jsonContent = await File.ReadAllTextAsync(pokeFile);
        }
        else
        {
            var url = $"https://pokeapi.co/api/v2/pokemon/{id}";
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            jsonContent = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(pokeFile, jsonContent);
        }

        // --- Basic info ---
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        var name = FormatIdentifier(root.GetProperty("name").GetString());
        var types = root.GetProperty("types").EnumerateArray()
            .Select(e =>
                Enum.Parse<PokemonType>(
                    FormatIdentifier(e.GetProperty("type").GetProperty("name").GetString())));
        var baseExperience = root.GetProperty("base_experience").GetUInt16();
        var stats = new DatabaseV2.StatsTableSchema(root.GetProperty("stats").EnumerateArray()
            .Select(e => e.GetProperty("base_stat").GetByte()));
        var height = root.GetProperty("height").GetUInt16();
        var weight = root.GetProperty("weight").GetUInt16();
        var abilities = ProcessAbilities(root.GetProperty("abilities"));
        var learnset = ProcessMoves(name, root.GetProperty("moves"));

        // --- Species info ---
        var speciesUrl = root.GetProperty("species").GetProperty("url").GetString()!;
        var speciesResponse = await HttpClient.GetAsync(speciesUrl);
        speciesResponse.EnsureSuccessStatusCode();
        var speciesJson = await speciesResponse.Content.ReadAsStringAsync();

        using var speciesDoc = JsonDocument.Parse(speciesJson);
        var baseHappiness = speciesDoc.RootElement.GetProperty("base_happiness").GetByte();
        var catchRate = speciesDoc.RootElement.GetProperty("capture_rate").GetByte();
        var growthRate = Enum.Parse<ExperienceGroup>(
            FormatIdentifier(speciesDoc.RootElement.GetProperty("growth_rate").GetProperty("name").GetString()!));
        var genderRate = speciesDoc.RootElement.GetProperty("gender_rate").GetSByte();

        // --- Evolution info ---
        var evolutionChainUrl = speciesDoc.RootElement.GetProperty("evolution_chain").GetProperty("url").GetString()!;
        var evolutionChainResponse = await HttpClient.GetAsync(evolutionChainUrl);
        evolutionChainResponse.EnsureSuccessStatusCode();
        var evolutionChainJson = await evolutionChainResponse.Content.ReadAsStringAsync();

        using var evolutionChainDoc = JsonDocument.Parse(evolutionChainJson);
        var baseEvolutionChainLink = evolutionChainDoc.RootElement.GetProperty("chain");

        var evolution = ProcessEvolutions(baseEvolutionChainLink, id);

        var schema = new DatabaseV2.PokemonSchema
        {
            Identifier = name,
            Types = types.ToList(),
            BaseHappiness = baseHappiness,
            CatchRate = catchRate,
            BaseExp = baseExperience,
            GrowthRate = growthRate,
            BaseStats = stats,
            Evolution = evolution,
            GenderRatio = genderRate,
            Height = height,
            Weight = weight,
            Abilities = abilities,
            LevelUpLearnset = learnset
        };

        return schema;
    }

    private static List<DatabaseV2.LevelEntrySchema> ProcessMoves(string name, JsonElement movesArray)
    {
        // Preferred version order: Scarlet/Violet -> Sword/Shield -> Ultra Sun/Ultra Moon
        string[] versionPriority = ["scarlet-violet", "sword-shield", "ultra-sun-ultra-moon"];

        foreach (var version in versionPriority)
        {
            var learnset = ExtractLearnsetForVersion(movesArray, version);

            if (learnset.Count > 0)
                return learnset;
        }

        Console.WriteLine($"!!! WARNING! EMPTY LEARNSET FOR {name} !!!");

        return [];
    }

    private static List<DatabaseV2.LevelEntrySchema> ExtractLearnsetForVersion(JsonElement movesArray,
        string versionGroupName)
    {
        var learnset = new List<DatabaseV2.LevelEntrySchema>();

        foreach (var movesEntry in movesArray.EnumerateArray())
        {
            foreach (var versionGroupDetails in movesEntry.GetProperty("version_group_details").EnumerateArray())
            {
                var versionName = versionGroupDetails.GetProperty("version_group").GetProperty("name").GetString();
                var learnMethod = versionGroupDetails.GetProperty("move_learn_method").GetProperty("name").GetString();

                if (versionName == versionGroupName && learnMethod == "level-up")
                {
                    var move = Enum.Parse<MoveID>(
                        FormatIdentifier(movesEntry.GetProperty("move").GetProperty("name").GetString()));
                    var levelLearned = versionGroupDetails.GetProperty("level_learned_at").GetByte();

                    learnset.Add(new DatabaseV2.LevelEntrySchema
                    {
                        AtLevel = levelLearned,
                        ID = (ushort)move
                    });
                }
            }
        }

        return learnset;
    }

    private static DatabaseV2.AbilitiesSchema ProcessAbilities(JsonElement abilitiesArray)
    {
        var ability1 = AbilityID.None;
        var ability2 = AbilityID.None;
        var hidden = AbilityID.None;

        foreach (var abilityEntry in abilitiesArray.EnumerateArray())
        {
            var abilityName = FormatIdentifier(
                abilityEntry.GetProperty("ability").GetProperty("name").GetString());
            var parsedAbility = Enum.Parse<AbilityID>(abilityName);
            var isHidden = abilityEntry.GetProperty("is_hidden").GetBoolean();
            var slot = abilityEntry.GetProperty("slot").GetInt32();

            if (isHidden)
            {
                hidden = parsedAbility;
            }
            else
                switch (slot)
                {
                    case 1:
                        ability1 = parsedAbility;
                        break;
                    case 2:
                        ability2 = parsedAbility;
                        break;
                }
        }

        return new DatabaseV2.AbilitiesSchema(ability1, ability2, hidden);
    }

    private static DatabaseV2.LevelEntrySchema? ProcessEvolutions(JsonElement chainLink, int currentPokemonId)
    {
        // Get the species info from current chain link
        var speciesUrl = chainLink.GetProperty("species").GetProperty("url").GetString()!;

        var pokemonId = ExtractIdFromUrl(speciesUrl);

        if (pokemonId == currentPokemonId)
        {
            // Process all evolutions from this Pokémon and return the first one found
            var evolution = ProcessEvolutionsFromPokemon(chainLink);
            if (evolution != null)
                return evolution;
        }

        // Recursively processes all evolution branches in the chain
        if (chainLink.TryGetProperty("evolves_to", out var evolvesToArray))
        {
            foreach (var evolution in evolvesToArray.EnumerateArray())
            {
                var result = ProcessEvolutions(evolution, currentPokemonId);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    private static DatabaseV2.LevelEntrySchema? ProcessEvolutionsFromPokemon(JsonElement chainLink)
    {
        if (!chainLink.TryGetProperty("evolves_to", out var evolvesToArray))
            return null;

        foreach (var evolution in evolvesToArray.EnumerateArray())
        {
            var toSpeciesUrl = evolution.GetProperty("species").GetProperty("url").GetString()!;
            var toPokemonId = ExtractIdFromUrl(toSpeciesUrl);

            // Process evolution details
            if (evolution.TryGetProperty("evolution_details", out var evolutionDetailsArray))
            {
                foreach (var detail in evolutionDetailsArray.EnumerateArray())
                {
                    // Checks if this is a level-up evolution
                    if (detail.TryGetProperty("trigger", out var trigger) &&
                        trigger.TryGetProperty("name", out var triggerName) &&
                        triggerName.GetString() == "level-up")
                    {
                        // Get the minimum level required
                        if (detail.TryGetProperty("min_level", out var minLevelElement) &&
                            minLevelElement.ValueKind != JsonValueKind.Null)
                        {
                            var minLevel = minLevelElement.GetInt32();

                            // Return the first evolution found
                            return new DatabaseV2.LevelEntrySchema((ushort)toPokemonId, (byte)minLevel);
                        }
                    }
                }
            }
        }

        return null;
    }

    private static int ExtractIdFromUrl(string url)
    {
        // Extracts the ID from a URL like "https://pokeapi.co/api/v2/pokemon-species/1/"
        var segments = url.TrimEnd('/').Split('/');
        return int.Parse(segments[^1]);
    }

    private static string FormatIdentifier(string? input)
    {
        var formatted = InvariantTextInfo
            .ToTitleCase(input!)
            .Replace("-", "");

        return IdentifierMappings.GetValueOrDefault(formatted, formatted);
    }
}