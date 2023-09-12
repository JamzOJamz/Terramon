using System;
using System.Collections.Generic;
using System.IO;
using Terramon.ID;
using Terraria.Localization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable once MemberCanBePrivate.Global

namespace Terramon.Content.Databases;

public class PokemonDB
{
    public const int FileVersion = 1;
    private const uint FileMagic = 0x6e6f6d74;

    public Dictionary<ushort, PokemonSchema> Pokemon { get; private init; }

    private ushort StarterMax { get; init; }

    public static PokemonDB Deserialize(Stream stream)
    {
        var reader = new BinaryReader(stream);
        if (reader.ReadUInt32() != FileMagic) throw new Exception("Invalid database file provided to deserializer");
        if (reader.Read7BitEncodedInt() > FileVersion) throw new Exception("Database file version unsupported");
        var database = new PokemonDB
        {
            StarterMax = (ushort)reader.Read7BitEncodedInt(),
            Pokemon = new Dictionary<ushort, PokemonSchema>()
        };
        var pokemonCount = reader.Read7BitEncodedInt();
        for (var i = 0; i < pokemonCount; i++)
        {
            var pokemon = new PokemonSchema
            {
                ID = (ushort)reader.Read7BitEncodedInt(),
                Name = reader.ReadString(),
                GenderRate = reader.ReadSByte(),
                Types = new List<byte>()
            };
            var typesCount = reader.ReadByte();
            for (var j = 0; j < typesCount; j++) pokemon.Types.Add(reader.ReadByte());

            var hasStats = reader.ReadBoolean();
            if (hasStats)
                pokemon.Stats = new StatsSchema
                {
                    HP = reader.ReadByte(),
                    Attack = reader.ReadByte(),
                    Defense = reader.ReadByte(),
                    SpAtk = reader.ReadByte(),
                    SpDef = reader.ReadByte(),
                    Speed = reader.ReadByte()
                };

            var hasEvolution = reader.ReadBoolean();
            if (hasEvolution)
                pokemon.Evolution = new EvolutionSchema
                {
                    ID = (ushort)reader.Read7BitEncodedInt(),
                    AtLevel = reader.ReadByte()
                };

            pokemon.NPC = new NPCSchema
            {
                Width = reader.ReadByte(),
                Height = reader.ReadByte(),
                AIInfo = new AIInfo
                {
                    Parameters = new List<object>()
                }
            };
            BehaviourID.Search.TryGetName(reader.ReadByte(), out var behaviourName);
            pokemon.NPC.AIInfo.Type = behaviourName;
            var aiParamCount = reader.ReadByte();
            for (var j = 0; j < aiParamCount; j++)
            {
                var paramKind = reader.ReadByte();
                var paramValue = paramKind switch
                {
                    ParameterKind.Int => reader.ReadInt32(),
                    ParameterKind.Float => reader.ReadSingle(),
                    _ => Type.Missing
                };
                pokemon.NPC.AIInfo.Parameters.Add(paramValue);
            }

            var hasSpawnInfo = reader.ReadBoolean();
            if (hasSpawnInfo)
            {
                pokemon.NPC.SpawnInfo = new SpawnInfo();
                var conditionCount = reader.ReadByte();
                pokemon.NPC.SpawnInfo.Conditions = new byte[conditionCount];
                for (var j = 0; j < conditionCount; j++) pokemon.NPC.SpawnInfo.Conditions[j] = reader.ReadByte();
                pokemon.NPC.SpawnInfo.Chance = reader.ReadSingle();
            }

            database.Pokemon.Add(pokemon.ID, pokemon);
        }

        reader.Close();

        return database;
    }

    public PokemonSchema GetPokemon(ushort id)
    {
        return Pokemon.TryGetValue(id, out var pokemon) ? pokemon : null;
    }

    public string GetPokemonName(ushort id)
    {
        return GetPokemon(id)?.Name;
    }

    public LocalizedText GetLocalizedPokemonName(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Name}.DisplayName");
    }

    public LocalizedText GetPokemonSpecies(ushort id)
    {
        return Language.GetText($"Mods.Terramon.Pokemon.{GetPokemon(id)?.Name}.Species");
    }

    public bool IsAvailableStarter(ushort id)
    {
        return id <= StarterMax;
    }

    public class PokemonSchema
    {
        public ushort ID { get; init; }
        public string Name { get; init; }
        public sbyte GenderRate { get; init; }
        public List<byte> Types { get; init; }
        public StatsSchema Stats { get; set; }
        public EvolutionSchema Evolution { get; set; }
        public NPCSchema NPC { get; set; }
    }

    public class StatsSchema
    {
        public byte HP { get; init; }
        public byte Attack { get; set; }
        public byte Defense { get; set; }
        public byte SpAtk { get; set; }
        public byte SpDef { get; set; }
        public byte Speed { get; set; }
    }

    public class EvolutionSchema
    {
        public ushort ID { get; set; }
        public byte AtLevel { get; set; }
    }

    public class NPCSchema
    {
        public byte Width { get; init; }
        public byte Height { get; init; }
        public AIInfo AIInfo { get; init; }
        public SpawnInfo SpawnInfo { get; set; }
    }

    public class AIInfo
    {
        public string Type { get; set; }
        public List<object> Parameters { get; init; }
    }

    private static class ParameterKind
    {
        public const byte Int = 1;
        public const byte Float = 2;
    }

    public class SpawnInfo
    {
        public byte[] Conditions { get; set; }
        public float Chance { get; set; }
    }
}