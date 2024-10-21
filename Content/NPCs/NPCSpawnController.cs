using System;
using System.Collections.Generic;
using Terramon.Content.Configs;
using Terramon.Core.NPCComponents;
using Terraria.ID;
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

    // Dictionary to track the counts of each SpawnCondition
    private static readonly Dictionary<string, int> ConditionCount = new();

    // Continue to support legacy system for setting spawn conditions
    public float Chance;
    public string Condition;

    /// <summary>
    ///     All possible conditions for the NPC to spawn and their respective chances. If any of these conditions are met, the
    ///     NPC will be added to the spawn pool with the specified chance.
    /// </summary>
    /// <remarks>Only the first condition that is met will be the one that is used.</remarks>
    public CustomCondition[] Conditions;

    protected override bool CacheInstances => true;

    public override void SetDefaults(NPC npc)
    {
        if (!Instances.ContainsKey(npc.type) && !string.IsNullOrEmpty(Condition) && Chance > 0)
            // Update the condition count dictionary
            if (!ConditionCount.TryAdd(Condition, 1))
                ConditionCount[Condition]++;

        base.SetDefaults(npc);
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (!ModContent.GetInstance<GameplayConfig>().AllowPokemonSpawning) return;

        // Check for some player buffs that affect spawn rates
        // TODO: Use reflection to access NPC.spawnRate instead
        var hasWaterCandle = spawnInfo.Player.HasBuff(BuffID.WaterCandle);
        var hasBattlePotion = spawnInfo.Player.HasBuff(BuffID.Battle);

        var typesAdded = new HashSet<int>();
        foreach (var (type, component) in Instances)
        {
            if (!component.Enabled) continue;
            var spawnController = (NPCSpawnController)component;
            if (!string.IsNullOrEmpty(spawnController.Condition) && spawnController.Chance > 0)
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
            }
        }

        // Normalize the spawn pool
        var totalTypesAdded = typesAdded.Count;
        foreach (var type in typesAdded)
            pool[type] /= totalTypesAdded;
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

        // Divides the spawn chance by the condition count to provide a more balanced spawn rate for Pokémon
        // Also takes into account the player's buffs, reducing the spawn chance if they have a Water Candle or Battle Potion
        var finalComputedChance = condition.Chance * spawnController.Chance /
                                  ConditionCount[spawnController.Condition] *
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