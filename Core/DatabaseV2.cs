using System.Collections.ObjectModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terramon.ID;
using Terraria.Localization;

// ReSharper disable InconsistentNaming

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Terramon.Core;

[JsonConverter(typeof(MultiPropertyNameConverter))]
public class DatabaseV2
{
/*
    [JsonPropertyAlias("sm")]
    public ushort StarterMax { get; init; }
*/

    [JsonPropertyAlias("p")] public ReadOnlyDictionary<ushort, PokemonSchema> Pokemon { get; init; }

    public static DatabaseV2 Parse(Stream stream)
    {
        var reader = new StreamReader(stream);
        var db = JsonConvert.DeserializeObject<DatabaseV2>(reader.ReadToEnd());
        reader.Close();
        return db;
    }

    public PokemonSchema GetPokemon(ushort id)
    {
        return Pokemon.GetValueOrDefault(id);
    }

    public string GetPokemonName(ushort id)
    {
        return GetPokemon(id)?.Identifier;
    }

    public LocalizedText GetLocalizedPokemonName(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DisplayName");
    }

    public static LocalizedText GetLocalizedPokemonName(PokemonSchema schema)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{schema.Identifier}.DisplayName");
    }

    public string GetLocalizedPokemonNameDirect(ushort id)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DisplayName");
    }

    public static string GetLocalizedPokemonNameDirect(PokemonSchema schema)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{schema.Identifier}.DisplayName");
    }

/*
    public LocalizedText GetPokemonSpecies(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.Species");
    }
*/

    public string GetPokemonSpeciesDirect(ushort id)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.Species");
    }

    public static string GetPokemonSpeciesDirect(PokemonSchema schema)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{schema.Identifier}.Species");
    }

/*
    public LocalizedText GetPokemonDexEntry(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DexEntry");
    }
*/

    public string GetPokemonDexEntryDirect(ushort id)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DexEntry");
    }

    public ushort GetEvolutionAtLevel(ushort id, byte level)
    {
        var pokemon = GetPokemon(id);
        if (pokemon?.Evolution == null) return 0;
        return pokemon.Evolution.AtLevel <= level && id != pokemon.Evolution.ID ? pokemon.Evolution.ID : (ushort)0;
    }

/*
    public bool IsAvailableStarter(ushort id)
    {
        return id <= StarterMax;
    }
*/

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public record PokemonSchema(
        [property: JsonProperty("name")]
        [property: JsonPropertyAlias("n")]
        string Identifier,
        [property: JsonPropertyAlias("t")] List<PokemonType> Types,
        [property: JsonPropertyAlias("c")] byte CatchRate,
        [property: JsonPropertyAlias("b")] ushort BaseExp,
        [property: JsonPropertyAlias("r")] ExperienceGroup GrowthRate,
        [property: JsonPropertyAlias("s")] StatsSchema Stats,
        [property: JsonPropertyAlias("e")] EvolutionSchema Evolution,
        [property: JsonProperty("genderRate")]
        [property: JsonPropertyAlias("g")]
        sbyte GenderRatio,
        [property: JsonPropertyAlias("h")] ushort Height,
        [property: JsonPropertyAlias("w")] ushort Weight
    )
    {
        public PokemonSchema() : this(
            string.Empty,
            [],
            45,
            45,
            ExperienceGroup.MediumFast,
            new StatsSchema(),
            new EvolutionSchema(),
            -1,
            0,
            0
        )
        {
        }
    }

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public record StatsSchema(
        [property: JsonProperty("hp")]
        [property: JsonPropertyAlias("h")]
        byte HP,
        [property: JsonPropertyAlias("a")] byte Attack,
        [property: JsonPropertyAlias("d")] byte Defense,
        [property: JsonPropertyAlias("sa")] byte SpAtk,
        [property: JsonPropertyAlias("sd")] byte SpDef,
        [property: JsonPropertyAlias("s")] byte Speed
    )
    {
        public StatsSchema() : this(0, 0, 0, 0, 0, 0)
        {
        }
    }

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public record EvolutionSchema(
        [property: JsonProperty("id")]
        [property: JsonPropertyAlias("i")]
        ushort ID,
        [property: JsonPropertyAlias("l")] byte AtLevel
    )
    {
        public EvolutionSchema() : this(0, 0)
        {
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal class JsonPropertyAliasAttribute(params string[] aliases) : Attribute
{
    public string[] Aliases { get; } = aliases;
}

internal class MultiPropertyNameConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var target = Activator.CreateInstance(objectType);

        foreach (var property in objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                          BindingFlags.Instance))
        {
            var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
            var aliases = property.GetCustomAttribute<JsonPropertyAliasAttribute>();

            var propertyNames = new List<string> { property.Name };

            if (jsonProperty != null && !string.IsNullOrEmpty(jsonProperty.PropertyName))
                propertyNames.Add(jsonProperty.PropertyName);

            if (aliases != null)
                propertyNames.AddRange(aliases.Aliases);

            JToken token = null;
            foreach (var name in propertyNames)
            {
                token = jObject[name];
                if (token != null) break;
            }

            if (token == null || !property.CanWrite) continue;

            var value = token.ToObject(property.PropertyType, serializer);
            property.SetValue(target, value);
        }

        return target;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // Use default serialization for writing
        var jObject = JObject.FromObject(value, serializer);
        jObject.WriteTo(writer);
    }
}