using Terramon.Content.Configs;
using Terramon.Core.NPCComponents;
using Terramon.ID;
using Terraria.ModLoader.Utilities;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to adjust the spawn location and rate of an NPC.
/// </summary>
public class NPCSpawnController : NPCComponent
{
    /// <summary>
    ///     A constant multiplier for balancing the spawn rates of Pokémon to vanilla NPCs.
    ///     Best value determined through testing, may need to be adjusted.
    /// </summary>
    private const float ConstantSpawnMultiplier = 1.25f; // Seems to work well

    // See https://terrariamods.wiki.gg/wiki/Terramon_Mod/Pok%C3%A9mon#Spawning
    private static readonly Dictionary<PokemonType, Func<NPCSpawnInfo, bool>> SimpleSpawnConditions = new()
    {
        {
            PokemonType.Normal, info => info.Player.ZoneForest // Forest
        },
        {
            PokemonType.Fire, info => info.Player.ZoneUnderworldHeight || info.Player.ZoneMeteor // Hell or Meteorites
        },
        {
            PokemonType.Fighting, info => info.Player.ZoneMeteor || info.Player.ZoneDungeon // Meteorites or Dungeon
        },
        {
            PokemonType.Water, info => info.Player.ZoneBeach || info.Player.ZoneRain // Beach or Rain
        },
        {
            PokemonType.Flying, info => info.Player.ZoneOverworldHeight || info.Player.ZoneSkyHeight // Surface or Space
        },
        {
            PokemonType.Grass, info => info.Player.ZoneJungle || info.Player.ZoneForest // Jungle or Forest
        },
        {
            PokemonType.Poison,
            info => info.Player.ZoneJungle || info.Player.ZoneCorrupt ||
                    info.Player.ZoneCrimson // Jungle or Evil biomes
        },
        {
            PokemonType.Electric, info => info.Player.ZoneRain // Rain
        },
        {
            PokemonType.Ground,
            info => info.Player.ZoneUndergroundDesert || info.Player.ZoneDesert ||
                    info.Player.ZoneDirtLayerHeight // Desert or Underground
        },
        {
            PokemonType.Psychic,
            info => info.Player.ZoneGlowshroom || info.Player.ZoneGemCave ||
                    info.Player.ZoneMarble // Glowing Mushroom biomes, Gem Caves, or Marble Caves
        },
        {
            PokemonType.Rock,
            info => info.Player.ZoneRockLayerHeight || info.Player.ZoneMeteor || info.Player.ZoneMarble ||
                    info.Player.ZoneGranite // Underground, Meteorites, Marble Caves, or Granite Caves
        },
        {
            PokemonType.Ice, info => info.Player.ZoneSnow // Snow
        },
        {
            PokemonType.Bug,
            info => info.Player.ZoneForest || info.Player.ZoneJungle || info.Player.ZoneRain // Forest, Jungle, or Rain
        },
        {
            PokemonType.Dragon,
            info => info.Player.ZoneSkyHeight ||
                    (info.Player.ZoneRain && info.Player.ZoneHallow) // Space or Hallow during Rain
        },
        {
            PokemonType.Ghost, info => info.Player.ZoneGraveyard || !Main.dayTime // Graveyard or Night
        },
        {
            PokemonType.Dark, info => info.Player.ZoneCorrupt || info.Player.ZoneCrimson // Evil biomes
        },
        {
            PokemonType.Steel,
            info => info.Player.ZoneUnderworldHeight || info.Player.ZoneGranite ||
                    info.Player.ZoneMeteor // Hell, Granite Caves, or Meteorites
        },
        {
            PokemonType.Fairy,
            info => info.Player.ZoneHallow || info.Player.ZoneGlowshroom // Hallow or Glowing Mushroom biomes
        }
    };

    // Continue to support legacy system for setting spawn conditions
    public float Chance;
    public string Condition;

    // Simple spawning system fields
    public SpawningStage Stage;

    /// <summary>
    ///     All possible conditions for the NPC to spawn and their respective chances. If any of these conditions are met, the
    ///     NPC will be added to the spawn pool with the specified chance.
    /// </summary>
    /// <remarks>Only the first condition that is met will be the one that is used.</remarks>
    //public CustomCondition[] Conditions;

    protected override bool CacheInstances => true;

    /*public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        Main.NewText($"spawnRate: {spawnRate}, maxSpawns: {maxSpawns}");
    }*/

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        var gameplayConfig = ModContent.GetInstance<GameplayConfig>();

        var regularSpawnRateMultiplier = gameplayConfig.NonPokemonSpawnRateMultiplier;
        if (regularSpawnRateMultiplier != 1f)
            // Iterate through all the NPCs in the pool and apply the spawn rate multiplier directly
            foreach (var key in pool.Keys.ToList())
                pool[key] *= regularSpawnRateMultiplier;

        var spawnRateMultiplier = gameplayConfig.PokemonSpawnRateMultiplier;
        if (spawnRateMultiplier == 0) return;

        // Check for some player buffs that affect spawn rates
        // TODO: Use reflection to access NPC.spawnRate instead
        var hasWaterCandle = spawnInfo.Player.HasBuff(BuffID.WaterCandle);
        var hasBattlePotion = spawnInfo.Player.HasBuff(BuffID.Battle);

        var typesAdded = new HashSet<int>();
        foreach (var (type, component) in Instances)
        {
            if (!component.Enabled) continue;
            var spawnController = (NPCSpawnController)component;
            if (spawnController.Stage > gameplayConfig.SpawningStage) continue;

            // Use simple Pokémon spawning system based on type for now
            // TODO: Implement a more complex system with unique spawn conditions for each Pokémon
            if (SimpleEditSpawnPool(pool, type, spawnController, spawnInfo, hasWaterCandle, hasBattlePotion))
                typesAdded.Add(type);

            /*if (!string.IsNullOrEmpty(spawnController.Condition) && spawnController.Chance > 0)
            {
                if (LegacyEditSpawnPool(pool, type, spawnController, hasWaterCandle, hasBattlePotion))
                    typesAdded.Add(type);
                continue;
            }

            if (spawnController.Conditions == null) continue;
            foreach (var condition in spawnController.Conditions)
            {
                if (condition.Overworld.HasValue &&
                    condition.Overworld.Value != spawnInfo.SpawnTileY <= Main.worldSurface)
                    continue;
                if (condition.DayTime.HasValue && condition.DayTime.Value != Main.dayTime)
                    continue;
                pool[type] = condition.Chance * (hasWaterCandle ? 0.66f : hasBattlePotion ? 0.5f : 1f) *
                             ConstantSpawnMultiplier;
                typesAdded.Add(type);
                break;
            }*/
        }

        // Normalize the spawn pool
        var totalTypesAdded = typesAdded.Count;
        foreach (var type in typesAdded)
            pool[type] = pool[type] / totalTypesAdded * spawnRateMultiplier;
    }

    private static bool SimpleEditSpawnPool(IDictionary<int, float> pool, int type, NPCSpawnController spawnController,
        NPCSpawnInfo spawnInfo, bool hasWaterCandle, bool hasBattlePotion)
    {
        const float chanceMultiplier = 7f / 32f; // 0.21875f
        var spawnChance = 0f;
        var schema = ((PokemonNPC)spawnController.NPC.ModNPC).Schema;
        var primaryType = schema.Types[0];
        if (SimpleSpawnConditions.TryGetValue(primaryType, out var primaryCondition) && primaryCondition(spawnInfo))
            spawnChance = 1f;

        var dualType = schema.Types.Count > 1;
        if (dualType)
        {
            var secondaryType = schema.Types[1];
            if (secondaryType != PokemonType.Flying &&
                SimpleSpawnConditions.TryGetValue(secondaryType, out var secondaryCondition) &&
                secondaryCondition(spawnInfo))
                spawnChance += 0.5f;
        }

        // Set the spawn chance for the Pokémon NPC in the spawn pool
        pool[type] = spawnChance * chanceMultiplier * (hasWaterCandle ? 0.66f : hasBattlePotion ? 0.5f : 1f);

        return spawnChance != 0;
    }

    private static bool LegacyEditSpawnPool(IDictionary<int, float> pool, int type, NPCSpawnController spawnController,
        bool hasWaterCandle, bool hasBattlePotion)
    {
        // Get the condition from the spawn controller component
        var condition = spawnController.Condition switch
        {
            "OverworldDaySlime" => SpawnCondition.OverworldDaySlime,
            _ => throw new NotImplementedException("Condition not implemented.")
        };

        // Compute the final spawn chance for the Pokémon NPC
        // Takes into account the player's buffs, reducing the spawn chance if they have a Water Candle or Battle Potion
        var finalComputedChance = condition.Chance * spawnController.Chance *
                                  (hasWaterCandle ? 0.66f : hasBattlePotion ? 0.5f : 1f) *
                                  ConstantSpawnMultiplier;

        // If the final computed chance is less than or equal to 0, don't add the NPC to the spawn pool
        if (finalComputedChance <= 0) return false;

        // Add the NPC to the spawn pool
        pool[type] = finalComputedChance;
        return true;
    }

    public class CustomCondition
    {
        /// <summary>
        ///     The chance of the NPC spawning, provided the condition is met.
        /// </summary>
        public float Chance;

        #region Criteria

        public bool? Overworld;

        public bool? DayTime;

        #endregion
    }
}