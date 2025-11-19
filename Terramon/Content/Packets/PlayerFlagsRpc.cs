using EasyPacketsLib;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing various player flags (booleans) with all clients.
/// </summary>
public struct PlayerFlagsRpc(byte player, bool starterChosen) : IEasyPacket
{
    private byte _player = player;
    private bool _starterChosen = starterChosen;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_starterChosen);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _player = reader.ReadByte();
        _starterChosen = reader.ReadBoolean();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received PlayerFlagsRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {_player}");
        var player = Main.player[_player].GetModPlayer<TerramonPlayer>();
        player.HasChosenStarter = _starterChosen;
        handled = true;
    }
}