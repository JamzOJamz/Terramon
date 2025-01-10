using EasyPacketsLib;
using Terramon.Content.NPCs;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class PokeClearCommand : DebugCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "pokeclear";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.PokeClear.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.PokeClear.Usage");

    protected override int MinimumArgumentCount => 0;

    public override void Load()
    {
        Mod.AddPacketHandler<PokeClearRpc>(OnPokeClearRpcReceived);
    }

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.PokeClear.Success", ClearPokemonNpcs()),
            ChatColorYellow);
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
            Main.NewText(
                Language.GetTextValue("Mods.Terramon.Commands.PokeClear.SuccessByPlayer",
                    Main.player[packet.ClearedByPlayer].name, clearedCount),
                ChatColorYellow);
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