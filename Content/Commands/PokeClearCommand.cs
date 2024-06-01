using System.IO;
using EasyPacketsLib;
using Terramon.Content.NPCs.Pokemon;
using Terraria.ID;

namespace Terramon.Content.Commands;

public class PokeClearCommand : TerramonCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "pokeclear";

    public override string Usage
        => "/pokeclear";

    public override string Description
        => "Clears all Pokémon NPCs in the world";

    protected override int MinimumArgumentCount => 0;

    public override void Load()
    {
        Mod.AddPacketHandler<PokeClearRpc>(OnPokeClearRpcReceived);
    }

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        caller.Reply($"Cleared {ClearPokemonNpcs()} Pokémon NPC(s)", new Color(255, 240, 20));
        if (Main.netMode != NetmodeID.Server) return;
        Mod.SendPacket(new PokeClearRpc((byte)caller.Player.whoAmI));
    }

    private static int ClearPokemonNpcs()
    {
        var clearCount = 0;
        foreach (var npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not PokemonNPC) continue;
            clearCount++;
            npc.active = false;
        }

        return clearCount;
    }

    private static void OnPokeClearRpcReceived(in PokeClearRpc packet, in SenderInfo sender, ref bool handled)
    {
        var clearedCount = ClearPokemonNpcs();
        if (Main.myPlayer != packet.ClearedByPlayer)
            Main.NewText($"{Main.player[packet.ClearedByPlayer].name} cleared {clearedCount} Pokémon NPC(s)",
                new Color(255, 240, 20));
        handled = true;
    }

    private readonly struct PokeClearRpc(byte clearedByPlayer) : IEasyPacket<PokeClearRpc>
    {
        public readonly byte ClearedByPlayer = clearedByPlayer;

        public void Serialise(BinaryWriter writer)
        {
            writer.Write(ClearedByPlayer);
        }

        public PokeClearRpc Deserialise(BinaryReader reader, in SenderInfo sender)
        {
            return new PokeClearRpc(reader.ReadByte());
        }
    }
}