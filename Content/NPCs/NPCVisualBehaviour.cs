using Terramon.Content.NPCs.Pokemon;
using Terramon.Core.NPCComponents;
using Terraria.DataStructures;
using Terraria.ID;

// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global

namespace Terramon.Content.NPCs;

/// <summary>
///     A <see cref="NPCComponent" /> to add variants to Pokemon NPCs.
/// </summary>
public class NPCVisualBehaviour : NPCComponent
{
    //TODO: Vector3 (or Color) and Vector2 support in hjson
    public Vector3 LightColor = Vector3.One;
    public float LightStrength = 0f;
    public float DamperAmount = 0; //0 = no effect, 1 = full effect

    public int DustID = -1;
    public float DustFrequency = 20;//how many frames until dust is spawned
    public Vector2 DustPosition;

    float dustTimer;
    Vector3 lightColor = Vector3.Zero;

    public override void AI(NPC npc)
    {
        base.AI(npc);

        if (LightStrength > 0)
            Lighting.AddLight(npc.position, LightColor * LightStrength * (Main.raining || npc.wet ? 1 - DamperAmount : 1));

        if (DustID > -1)
        {
            if (dustTimer >= DustFrequency)
            {
                Dust.NewDust(npc.position + DustPosition, 1, 1, DustID);
                dustTimer = 0;
            }
            else
                dustTimer++;
        }
    }
}