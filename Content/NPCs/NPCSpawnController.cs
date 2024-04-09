using System;
using System.Collections.Generic;
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
    public float Chance;
    public string Condition;

    protected override bool CacheInstances => true;

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        var hasWaterCandle = spawnInfo.Player.HasBuff(BuffID.WaterCandle);
        var hasBattlePotion = spawnInfo.Player.HasBuff(BuffID.Battle);
        foreach (var (type, component) in Instances)
        {
            if (!component.Enabled) continue;
            var spawnController = (NPCSpawnController)component;
            if (spawnController.Condition == string.Empty) continue;
            var condition = spawnController.Condition switch
            {
                "OverworldDaySlime" => SpawnCondition.OverworldDaySlime,
                _ => throw new NotImplementedException("Condition not implemented.")
            };
            pool[type] = condition.Chance * spawnController.Chance *
                         (hasWaterCandle ? 0.66f :
                             hasBattlePotion ? 0.5f : 1f);
        }
    }
}