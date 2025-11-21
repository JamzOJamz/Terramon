using Terramon.Core.NPCComponents;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to control the visual behaviour of Pokémon NPCs.
/// </summary>
public class NPCVisuals : NPCComponent
{
    private float _dustTimer;
    public float DamperAmount = 0; //0 = no effect, 1 = full effect
    public float DustFrequency = 20; //how many frames until dust is spawned
    public int DustID = -1;
    public float DustOffsetX = 0;
    public float DustOffsetY = 0;
    public Vector3 LightColor = Vector3.One;
    public float LightStrength = 0f;
    public Vector3 ShinyLightColor = Vector3.One;

    public override void AI(NPC npc)
    {
        base.AI(npc);

        PokemonNPC modNPC;
        if (!Enabled || (modNPC = npc.Pokemon()).PlasmaState) return;

        if (LightStrength > 0)
            Lighting.AddLight(npc.Center,
                (modNPC.Data is { IsShiny: true } ? ShinyLightColor : LightColor) * LightStrength *
                (Main.raining || npc.wet ? 1 - DamperAmount : 1));

        if (DustID <= -1) return;
        if (_dustTimer >= DustFrequency)
        {
            Dust.NewDustPerfect(
                npc.position + new Vector2(npc.spriteDirection == 1 ? npc.width - DustOffsetX : DustOffsetX,
                    DustOffsetY), DustID);
            _dustTimer = 0;
        }
        else
        {
            _dustTimer++;
        }
    }
}