using System.IO;
using Terramon.Core.NPCComponents;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace Terramon.Content.NPCs;

/// <summary>
///     A base <see cref="NPCComponent" /> for adding AI to an NPC.
///     This class is abstract and must be inherited.
/// </summary>
public abstract class NPCAIComponent : NPCComponent
{
    /// <summary>
    ///     A <see cref="FastRandom" /> for generating random numbers.
    ///     This is synchronized across the network and should be used to ensure deterministic randomness on all clients in
    ///     multiplayer.
    /// </summary>
    protected FastRandom Random;

    /// <summary>
    ///     The NPC this component is attached to.
    /// </summary>
    protected NPC NPC { get; private set; }
    
    protected override void OnEnabled(NPC npc)
    {
        NPC = npc;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (!Enabled || Main.netMode == NetmodeID.MultiplayerClient) return;

        Random = FastRandom.CreateWithRandomSeed();
        npc.netUpdate = true;
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (!Enabled) return;
        
        modifiers.FinalDamage *= float.Epsilon;

        npc.velocity.X = 0;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        if (!Enabled) return;

        binaryWriter.Write(Random.Seed);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        if (!Enabled) return;

        Random = new FastRandom(binaryReader.ReadUInt64());
    }
}