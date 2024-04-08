using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria.Localization;

namespace Terramon.Core;

public class DatabaseV2
{
    [JsonProperty]
    public int StarterMax { get; private set; }
    
    [JsonProperty]
    public Dictionary<int, PokemonSchema> Pokemon { get; private set; }
    
    public static DatabaseV2 Parse(Stream stream)
    {
        var reader = new StreamReader(stream);
        var db = JsonConvert.DeserializeObject<DatabaseV2>(reader.ReadToEnd());
        reader.Close();
        return db;
    }
    
    public PokemonSchema GetPokemon(ushort id)
    {
        return Pokemon.TryGetValue(id, out var pokemon) ? pokemon : null;
    }

    public string GetPokemonName(ushort id)
    {
        return GetPokemon(id)?.Identifier;
    }

    public LocalizedText GetLocalizedPokemonName(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DisplayName");
    }

    public LocalizedText GetPokemonSpecies(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.Species");
    }

    public bool IsAvailableStarter(ushort id)
    {
        return id <= StarterMax;
    }
    
    public class PokemonSchema
    {
        [JsonProperty("name")]
        public string Identifier { get; set; }
        public List<byte> Types { get; set; }
        public Stats Stats { get; set; }
        public Evolution Evolution { get; set; }
        public int GenderRate { get; set; }
    }
    
    public class Stats
    {
        [JsonProperty("hp")]
        public int HP { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpAtk { get; set; }
        public int SpDef { get; set; }
        public int Speed { get; set; }
    }

    public class Evolution
    {
        [JsonProperty("id")]
        public int ID { get; set; }
        public int AtLevel { get; set; }
    }
}