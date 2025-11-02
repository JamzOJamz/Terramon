using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Showdown.NET.Definitions;
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

    [JsonPropertyAlias("m")] public ReadOnlyDictionary<ushort, MoveSchema> Moves { get; init; }

    public static DatabaseV2 Parse(Stream stream)
    {
        var reader = new StreamReader(stream);
        var db = JsonConvert.DeserializeObject<DatabaseV2>(reader.ReadToEnd());
        reader.Close();
        return db;
    }

    public string Serialize(bool minify = false)
    {
        var prevValue = MultiPropertyNameConverter.GetUseAliases();
        try
        {
            MultiPropertyNameConverter.SetUseAliases(minify);
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                Formatting = minify ? Formatting.None : Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
        }
        finally
        {
            MultiPropertyNameConverter.SetUseAliases(prevValue);
        }
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

    public MoveSchema GetMove(ushort id)
    {
        return Moves.GetValueOrDefault(id);
    }

    public MoveSchema GetMove(MoveID id)
        => GetMove((ushort)id);
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
        [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("f")]
        [property: DefaultValue((byte)70)]
        byte BaseHappiness,
        [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("c")]
        [property: DefaultValue((byte)45)]
        byte CatchRate,
        [property: JsonPropertyAlias("b")] ushort BaseExp,
        [property: JsonPropertyAlias("r")]
        [property: DefaultValue(ExperienceGroup.MediumFast)]
        ExperienceGroup GrowthRate,
        [property: JsonPropertyAlias("s")] StatsTableSchema BaseStats,
        [property: JsonPropertyAlias("e")] LevelEntrySchema Evolution,
        [property: JsonProperty("genderRate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("g")]
        [property: DefaultValue((sbyte)4)]
        sbyte GenderRatio,
        [property: JsonPropertyAlias("h")] ushort Height,
        [property: JsonPropertyAlias("w")] ushort Weight,
        [property: JsonPropertyAlias("a")] AbilitiesSchema Abilities,
        [property: JsonProperty("moves")]
        [property: JsonPropertyAlias("m")]
        List<LevelEntrySchema> LevelUpLearnset
    )
    {
        public PokemonSchema() : this(
            string.Empty,
            [],
            70,
            45,
            0,
            ExperienceGroup.MediumFast,
            [],
            null,
            4,
            0,
            0,
            new AbilitiesSchema(),
            []
        )
        {
        }
    }

    public sealed class StatsTableSchema : List<byte>
    {
        public StatsTableSchema()
        {
        }

        public StatsTableSchema(IEnumerable<byte> values) : base(values)
        {
        }

        public byte HP => this[0];
        public byte Attack => this[1];
        public byte Defense => this[2];
        public byte SpAtk => this[3];
        public byte SpDef => this[4];
        public byte Speed => this[5];
        public byte DefenseEffort => this[8];
        public byte SpAtkEffort => this[9];
        public byte SpDefEffort => this[10];
        public byte SpeedEffort => this[11];

        public byte this[StatID stat] => this[(int)stat];
        public byte GetEffort(StatID stat) => this[(int)stat + 6];
    }

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public sealed record LevelEntrySchema(
        [property: JsonProperty("id")]
        [property: JsonPropertyAlias("i")]
        ushort ID,
        [property: JsonPropertyAlias("l")] byte AtLevel
    )
    {
        public LevelEntrySchema() : this(0, 0)
        {
        }
    }

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public sealed record AbilitiesSchema(
        [property: JsonProperty("1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: DefaultValue(AbilityID.None)]
        AbilityID Ability1,
        [property: JsonProperty("2", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: DefaultValue(AbilityID.None)]
        AbilityID Ability2,
        [property: JsonProperty("hidden", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("h")]
        [property: DefaultValue(AbilityID.None)]
        AbilityID Hidden
    )
    {
        public AbilitiesSchema() : this(AbilityID.None, AbilityID.None, AbilityID.None)
        {
        }
    }

    [JsonConverter(typeof(MultiPropertyNameConverter))]
    public sealed record MoveSchema(
        [property: JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("t")]
        [property: DefaultValue(PokemonType.Normal)]
        PokemonType Type,
        [property: JsonProperty("power", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("p")]
        byte? Power,
        [property: JsonProperty("pp")] byte PP,
        [property: JsonProperty("accuracy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("a")]
        byte? Accuracy,
        [property: JsonProperty("category", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("c")]
        [property: DefaultValue(MoveCategory.Physical)]
        MoveCategory Category,
        [property: JsonProperty("effect", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("e")]
        ushort? Effect,
        [property: JsonProperty("effectChance", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [property: JsonPropertyAlias("ec")]
        byte? EffectChance
    )
    {
        public MoveSchema() : this(PokemonType.Normal, 0, 0, 0, MoveCategory.Dynamic, 0, 0)
        {
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class JsonPropertyAliasAttribute(string alias) : Attribute
{
    public string Alias { get; } = alias;
}

internal class MultiPropertyNameConverter : JsonConverter
{
    // Thread-safe storage for the alias flag
    private static readonly AsyncLocal<bool> _useAliases = new();

    public static bool GetUseAliases() => _useAliases.Value;

    public static void SetUseAliases(bool useAliases)
    {
        _useAliases.Value = useAliases;
    }

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
            var alias = property.GetCustomAttribute<JsonPropertyAliasAttribute>();

            var propertyNames = new List<string> { property.Name };

            if (jsonProperty != null && !string.IsNullOrEmpty(jsonProperty.PropertyName))
                propertyNames.Add(jsonProperty.PropertyName);

            if (alias != null)
                propertyNames.Add(alias.Alias);

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
        if (value == null)
        {
            if (serializer.NullValueHandling != NullValueHandling.Ignore)
                writer.WriteNull();
            return;
        }

        var useAliases = _useAliases.Value;

        writer.WriteStartObject();

        var type = value.GetType();
        var contract = serializer.ContractResolver.ResolveContract(type) as JsonObjectContract;

        foreach (var property in type.GetProperties(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!property.CanRead)
                continue;

            var propValue = property.GetValue(value);

            if (propValue == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                continue;

            var jsonProperty = contract?.Properties.FirstOrDefault(p => p.UnderlyingName == property.Name);
            if (jsonProperty == null)
                continue;

            var hasIgnoreDefault = property.GetCustomAttributes<JsonPropertyAttribute>()
                .Any(attr => attr.DefaultValueHandling == DefaultValueHandling.Ignore);

            if (hasIgnoreDefault)
            {
                var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttr != null)
                {
                    var defaultValue = defaultValueAttr.Value;

                    if (Equals(propValue, defaultValue))
                        continue;
                }
            }

            var propertyName = useAliases
                ? property.GetCustomAttribute<JsonPropertyAliasAttribute>()?.Alias ?? jsonProperty.PropertyName!
                : jsonProperty.PropertyName!;

            writer.WritePropertyName(propertyName);
            serializer.Serialize(writer, propValue);
        }

        writer.WriteEndObject();
    }
}