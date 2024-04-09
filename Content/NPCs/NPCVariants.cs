using Terramon.Content.NPCs.Pokemon;
using Terramon.Core.NPCComponents;
using Terraria.DataStructures;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to add variants to Pokemon NPCs.
/// </summary>
public class NPCVariants : NPCComponent
{
    public string Kind;
    public float Chance;
    public string Condition;

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        base.OnSpawn(npc, source);
        var modNpc = (PokemonNPC)npc.ModNPC;

        //TODO: add array, iterate through each variant, etc.
        //also probably mp sync

        if (Kind == null) return;

        var condition = Condition switch
        {
            "Christmas" => Main.xMas ? 1 : 0,
            _ => 1 //if no condition given assume it should happen always
        };

        var random = Main.rand.NextFloat(0, 1);
        if (random > Chance * condition) return;

        modNpc.variant = Kind;
        npc.netUpdate = true;
    }
    
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.ModNPC is PokemonNPC;
    }
}