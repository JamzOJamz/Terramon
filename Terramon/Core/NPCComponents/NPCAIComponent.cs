using Terramon.Content.NPCs;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global

namespace Terramon.Core.NPCComponents;

/// <summary>
///     A base <see cref="NPCComponent" /> for adding AI to an NPC.
///     This class is abstract and must be inherited.
/// </summary>
public abstract class NPCAIComponent : NPCComponent
{
    public int FrameCount = 2;
    public int FrameTime = 10;
    
    /// <summary>
    ///     A <see cref="FastRandom" /> for generating random numbers.
    ///     This is synchronized across the network and should be used to ensure deterministic randomness on all clients in
    ///     multiplayer.
    /// </summary>
    protected FastRandom Random;

    /// <summary>
    ///     Shorthand for <c>((PokemonNPC)NPC.ModNPC).PlasmaState</c>.
    /// </summary>
    protected bool PlasmaState => ((PokemonNPC)NPC.ModNPC).PlasmaState;

    public override void SetDefaults(NPC npc)
    {
        base.SetDefaults(npc);
        if (!Enabled) return;

        Main.npcFrameCount[npc.type] = FrameCount;
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

    public override void ReceiveExtraAI(NPC npc, Terraria.ModLoader.IO.BitReader bitReader, BinaryReader binaryReader)
    {
        if (!Enabled) return;

        Random = new FastRandom(binaryReader.ReadUInt64());
    }
}