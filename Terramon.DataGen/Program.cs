using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
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
            //try
            {
                Console.WriteLine($"Fetching Pokémon ID {id}...");
                var pokemonSchema = await FetchSpeciesData(id);
                Console.WriteLine($"Fetched {pokemonSchema.BaseForm.Identifier} (ID {id}) successfully.\n");
                pokemon[id] = pokemonSchema;
            }
            //catch (Exception ex)
            {
                //Console.WriteLine($"Error fetching Pokémon {id}: {ex.Message}");
            }
        }
        
        var cacheDir = GetCacheDirectory();
        var csvPath = await MoveService.DownloadMovesCsv(cacheDir);
        var moves = MoveService.ProcessMovesCsv(csvPath);

        var databaseV2 = new DatabaseV2
        {
            Pokemon = new ReadOnlyDictionary<ushort, DatabaseV2.PokemonSchema>(pokemon),
            Moves = new ReadOnlyDictionary<ushort, DatabaseV2.MoveSchema>(moves)
        };

        var json = databaseV2.Serialize();
        var jsonMinified = databaseV2.Serialize(true);

        var outDir = Path.Combine(Environment.CurrentDirectory, "..");
        if (Exec)
            outDir = Path.Combine(outDir, "..", "..", "..");
        outDir = Path.Combine(outDir, "Terramon", "Assets", "Data");
        Directory.CreateDirectory(outDir);

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
        Directory.CreateDirectory(cacheDir);
        return cacheDir;
    }

    private static async Task<string> GetCachedUrlAsync(string filePath, string url)
    {
        // --- Handle caching in accordance to PokéAPI's fair use policy ---

        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }
        else
        {
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(filePath, jsonContent);
            return jsonContent;
        }
    }

    private static async Task<DatabaseV2.FormSchema> FetchFormData(string url, string? pokeCacheDir = null)
    {
        pokeCacheDir ??= GetCacheDirectory("Pokemon");

        var id = ExtractIdFromUrl(url);
        var pokeFile = Path.Combine(pokeCacheDir, $"{id}.pkmn");

        var jsonContent = await GetCachedUrlAsync(pokeFile, url);

        // --- Basic info ---
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        var name = FormatIdentifier(root.GetProperty("name").GetString());
        var types = root.GetProperty("types").EnumerateArray()
            .Select(e =>
                Enum.Parse<PokemonType>(
                    FormatIdentifier(e.GetProperty("type").GetProperty("name").GetString())));
        var baseExperience = root.GetProperty("base_experience");
        var statsArray = root.GetProperty("stats").EnumerateArray();
        var statsBuf = new byte[12];
        int cur = 0;
        foreach (var stat in statsArray)
            statsBuf[cur++] = stat.GetProperty("base_stat").GetByte();
        foreach (var stat in statsArray)
            statsBuf[cur++] = stat.GetProperty("effort").GetByte();
        var stats = new DatabaseV2.StatsTableSchema(statsBuf);
        var height = root.GetProperty("height").GetUInt16();
        var weight = root.GetProperty("weight").GetUInt16();
        var abilities = ProcessAbilities(root.GetProperty("abilities"));
        var learnset = ProcessMoves(name, root.GetProperty("moves"));

        return new DatabaseV2.FormSchema
        {
            Identifier = name,
            Types = types.ToList(),
            BaseExp = baseExperience.ValueKind == JsonValueKind.Null ? (ushort)0 : baseExperience.GetUInt16(),
            BaseStats = stats,
            Height = height,
            Weight = weight,
            Abilities = abilities,
            LevelUpLearnset = learnset,
        };
    }

    private static async Task<DatabaseV2.PokemonSchema> FetchSpeciesData(int id)
    {
        if (id > 10000)
            throw new ArgumentOutOfRangeException($"{id} isn't a valid species number.");

        // --- Handle caching in accordance to PokéAPI's fair use policy ---
        var pokeCacheDir = GetCacheDirectory("Pokemon");
        var speciesFile = Path.Combine(pokeCacheDir, $"{id}.pksp");
        var evolveFile = Path.Combine(pokeCacheDir, $"{id}.pkev");

        // --- Species info ---

        var jsonContent = await GetCachedUrlAsync(speciesFile, $"https://pokeapi.co/api/v2/pokemon-species/{id}");

        using var speciesDoc = JsonDocument.Parse(jsonContent);
        var root = speciesDoc.RootElement;

        var baseHappiness = root.GetProperty("base_happiness").GetByte();
        var catchRate = root.GetProperty("capture_rate").GetByte();
        var growthRate = Enum.Parse<ExperienceGroup>(
            FormatIdentifier(root.GetProperty("growth_rate").GetProperty("name").GetString()!));
        var genderRate = root.GetProperty("gender_rate").GetSByte();
        var forms = await ProcessForms(id, pokeCacheDir, root.GetProperty("varieties"));

        var baseForm = forms[0];
        var otherForms = new Dictionary<string, DatabaseV2.FormSchema>(forms.Count - 1);
        foreach (var form in CollectionsMarshal.AsSpan(forms)[1..])
        {
            // Since these are always linked to the base form, we can remove the original identifier
            // All forms start with the Pokemon name (identifier of baseForm)

            string newIdentifier = form.Identifier.Substring(baseForm.Identifier.Length);

            otherForms.Add(newIdentifier, form with { Identifier = newIdentifier } );
        }

        // --- Evolution info ---

        var evolutionChainJson = await GetCachedUrlAsync(evolveFile, root.GetProperty("evolution_chain").GetProperty("url").GetString()!);

        using var evolutionChainDoc = JsonDocument.Parse(evolutionChainJson);
        var baseEvolutionChainLink = evolutionChainDoc.RootElement.GetProperty("chain");

        var evolution = ProcessEvolutions(baseEvolutionChainLink, id);

        return new DatabaseV2.PokemonSchema
        {
            BaseHappiness = baseHappiness,
            CatchRate = catchRate,
            GrowthRate = growthRate,
            GenderRatio = genderRate,
            Evolution = evolution,
            BaseForm = baseForm,
            Forms = new ReadOnlyDictionary<string, DatabaseV2.FormSchema>(otherForms)
        };
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

    private static async Task<List<DatabaseV2.FormSchema>> ProcessForms(int id, string pokeCacheDir, JsonElement varietiesArray)
    {
        var forms = new List<DatabaseV2.FormSchema>();

        foreach (var form in varietiesArray.EnumerateArray())
        {
            var pokemon = form.GetProperty("pokemon");
            // TODO: Fetch others at some point
            if (!form.GetProperty("is_default").GetBoolean() && !pokemon.GetProperty("name").GetString()!.Contains("-mega"))
                continue;

            var formUrl = 
                pokemon.GetProperty("url").GetString()!;
            forms.Add(await FetchFormData(formUrl));
        }
        return forms;
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