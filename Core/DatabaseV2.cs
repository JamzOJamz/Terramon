using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Terramon.ID;
using Terraria.Localization;

// ReSharper disable InconsistentNaming

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Terramon.Core;

public class DatabaseV2
{
    [JsonProperty] public ushort StarterMax { get; private set; }

    [JsonProperty("sm")]
    private ushort b_StarterMax
    {
        set => StarterMax = value;
    }

    [JsonProperty] public ReadOnlyDictionary<ushort, PokemonSchema> Pokemon { get; private set; }

    [JsonProperty("p")]
    private ReadOnlyDictionary<ushort, PokemonSchema> b_Pokemon
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

    /*public LocalizedText GetPokemonSpecies(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.Species");
    }*/
    
    public string GetPokemonSpeciesDirect(ushort id)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.Species");
    }
    
    public static string GetPokemonSpeciesDirect(PokemonSchema schema)
    {
        return Language.GetTextValue($"Mods.Terramon.Pokemon.{schema.Identifier}.Species");
    }
    
    /*public LocalizedText GetPokemonDexEntry(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Identifier}.DexEntry");
    }*/
    
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

    public bool IsAvailableStarter(ushort id)
    {
        return id <= StarterMax;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class PokemonSchema
    {
        [JsonProperty("name")] public string Identifier { get; private set; }

        [JsonProperty("n")]
        private string b_Identifier
        {
            set => Identifier = value;
        }

        public List<PokemonType> Types { get; private set; }

        [JsonProperty("t")]
        private List<PokemonType> b_Types
        {
            set => Types = value;
        }
        
        public byte CatchRate { get; private set; } = 45;
        
        [JsonProperty("c")]
        private byte b_CatchRate
        {
            set => CatchRate = value;
        }

        public ushort BaseExp { get; private set; } = 45;
        
        [JsonProperty("b")]
        private ushort b_BaseExp
        {
            set => BaseExp = value;
        }
        
        public ExperienceGroup GrowthRate { get; private set; } = ExperienceGroup.MediumFast;
        
        [JsonProperty("r")]
        private ExperienceGroup b_GrowthRate
        {
            set => GrowthRate = value;
        }

        public StatsSchema Stats { get; private set; }

        [JsonProperty("s")]
        private StatsSchema b_Stats
        {
            set => Stats = value;
        }

        public EvolutionSchema Evolution { get; private set; }

        [JsonProperty("e")]
        private EvolutionSchema b_Evolution
        {
            set => Evolution = value;
        }
        
        [JsonProperty("genderRate")]
        public sbyte GenderRatio { get; private set; } = -1;

        [JsonProperty("g")]
        private sbyte b_GenderRatio
        {
            set => GenderRatio = value;
        }

        [JsonProperty("height")]
        public ushort Height { get; private set; }
        
        [JsonProperty("h")]
        private ushort b_Height
        {
            set => Height = value;
        }
        
        [JsonProperty("weight")]
        public ushort Weight { get; private set; }
        
        [JsonProperty("w")]
        private ushort b_Weight
        {
            set => Weight = value;
        }
    }

    public class StatsSchema
    {
        [JsonProperty("hp")] public byte HP { get; set; }

        [JsonProperty("h")]
        private byte b_HP
        {
            set => HP = value;
        }

        public byte Attack { get; set; }

        [JsonProperty("a")]
        private byte b_Attack
        {
            set => Attack = value;
        }

        public byte Defense { get; set; }

        [JsonProperty("d")]
        private byte b_Defense
        {
            set => Defense = value;
        }

        public byte SpAtk { get; set; }

        [JsonProperty("sa")]
        private byte b_SpAtk
        {
            set => SpAtk = value;
        }

        public byte SpDef { get; set; }

        [JsonProperty("sd")]
        private byte b_SpDef
        {
            set => SpDef = value;
        }

        public byte Speed { get; set; }

        [JsonProperty("s")]
        private byte b_Speed
        {
            set => Speed = value;
        }
    }

    public class EvolutionSchema
    {
        [JsonProperty("id")] public ushort ID { get; set; }

        [JsonProperty("i")]
        private ushort b_ID
        {
            set => ID = value;
        }

        public byte AtLevel { get; set; }

        [JsonProperty("l")]
        private byte b_AtLevel
        {
            set => AtLevel = value;
        }
    }
}