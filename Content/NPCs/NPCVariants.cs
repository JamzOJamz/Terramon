using System;
using System.Collections.Generic;
using Terramon.Content.NPCs.Pokemon;
using Terramon.Core.NPCComponents;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.Utilities;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to debug because idk how these things work.
/// </summary>
public class NPCVariants : NPCComponent
{
    public string FileName;
    public float Chance;
    public string Condition;

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        base.OnSpawn(npc, source);
        if (!(npc.ModNPC is PokemonNPC)) return;

        //TODO: add array, iterate through each variant, etc.
        //also probably mp sync

        if (FileName == null) return;

        var condition = Condition switch
        {
            "Christmas" => Main.xMas ? 1 : 0,
            _ => 1 //if no condition given assume it should happen always
        };

        var random = Main.rand.NextFloat(0, 1);
        if (random > Chance * condition) return;

        var modNpc = npc.ModNPC as PokemonNPC;
        modNpc.variant = FileName;
    }
}