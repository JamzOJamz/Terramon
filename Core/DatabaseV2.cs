using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria.Localization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Terramon.Core;

public class DatabaseV2
{
    [JsonProperty] public int StarterMax { get; private set; }

    [JsonProperty("sm")]
    private int b_StarterMax
    {
        set => StarterMax = value;
    }

    [JsonProperty] public Dictionary<int, PokemonSchema> Pokemon { get; private set; }

    [JsonProperty("p")]
    private Dictionary<int, PokemonSchema> b_Pokemon
    {
        set => Pokemon = value;
    }

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
        [JsonProperty("name")] public string Identifier { get; set; }

        [JsonProperty("n")]
        private string b_Identifier
        {
            set => Identifier = value;
        }

        public List<byte> Types { get; set; }

        [JsonProperty("t")]
        private List<byte> b_Types
        {
            set => Types = value;
        }

        public Stats Stats { get; set; }

        [JsonProperty("s")]
        private Stats b_Stats
        {
            set => Stats = value;
        }

        public Evolution Evolution { get; set; }

        [JsonProperty("e")]
        private Evolution b_Evolution
        {
            set => Evolution = value;
        }

        public int GenderRate { get; set; }

        [JsonProperty("g")]
        private int b_GenderRate
        {
            set => GenderRate = value;
        }
    }

    public class Stats
    {
        [JsonProperty("hp")] public int HP { get; set; }

        [JsonProperty("h")]
        private int b_HP
        {
            set => HP = value;
        }

        public int Attack { get; set; }

        [JsonProperty("a")]
        private int b_Attack
        {
            set => Attack = value;
        }

        public int Defense { get; set; }

        [JsonProperty("d")]
        private int b_Defense
        {
            set => Defense = value;
        }

        public int SpAtk { get; set; }

        [JsonProperty("sa")]
        private int b_SpAtk
        {
            set => SpAtk = value;
        }

        public int SpDef { get; set; }

        [JsonProperty("sd")]
        private int b_SpDef
        {
            set => SpDef = value;
        }

        public int Speed { get; set; }

        [JsonProperty("s")]
        private int b_Speed
        {
            set => Speed = value;
        }
    }

    public class Evolution
    {
        [JsonProperty("id")] public int ID { get; set; }

        [JsonProperty("i")]
        private int b_ID
        {
            set => ID = value;
        }

        public int AtLevel { get; set; }

        [JsonProperty("l")]
        private int b_AtLevel
        {
            set => AtLevel = value;
        }
    }
}