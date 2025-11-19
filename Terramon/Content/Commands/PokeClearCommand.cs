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

    private struct PokeClearRpc(byte clearedByPlayer) : IEasyPacket
    {
        public byte ClearedByPlayer = clearedByPlayer;

        public readonly void Serialise(BinaryWriter writer)
        {
            writer.Write(ClearedByPlayer);
        }
        public void Deserialise(BinaryReader reader, in SenderInfo sender)
        {
            ClearedByPlayer = reader.ReadByte();
        }
        public readonly void Receive(in SenderInfo sender, ref bool handled)
        {
            var clearedCount = ClearPokemonNpcs();
            if (Main.myPlayer != ClearedByPlayer)
                Main.NewText(
                    Language.GetTextValue("Mods.Terramon.Commands.PokeClear.SuccessByPlayer",
                        Main.player[ClearedByPlayer].name, clearedCount),
                    ChatColorYellow);
            handled = true;
        }
    }
}