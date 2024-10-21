using System.IO;
using EasyPacketsLib;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing various player flags (booleans) with all clients.
/// </summary>
public readonly struct PlayerFlagsRpc(byte player, bool starterChosen)
    : IEasyPacket<PlayerFlagsRpc>, IEasyPacketHandler<PlayerFlagsRpc>
{
    private readonly byte _player = player;
    private readonly bool _starterChosen = starterChosen;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_starterChosen);
    }

    public PlayerFlagsRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        return new PlayerFlagsRpc(reader.ReadByte(), reader.ReadBoolean());
    }

    public void Receive(in PlayerFlagsRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received PlayerFlagsRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.HasChosenStarter = packet._starterChosen;
        handled = true;
    }
}