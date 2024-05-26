using System.IO;
using Terramon.Core.Networking;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing a player's <see cref="TerramonPlayer.Party" /> with all clients.
/// </summary>
public readonly struct PartySyncRpc(byte player, byte index, PokemonData data)
    : IEasyPacket<PartySyncRpc>, IEasyPacketHandler<PartySyncRpc>
{
    private readonly byte _player = player;
    private readonly byte _index = index;
    private readonly PokemonData _data = data;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_index);
        writer.Write(_data != null);
        _data?.NetWrite(writer);
    }

    public PartySyncRpc Deserialize(BinaryReader reader, in SenderInfo sender)
    {
        var readPlayer = reader.ReadByte();
        var readIndex = reader.ReadByte();
        var readData = reader.ReadBoolean() ? new PokemonData().NetRead(reader) : null;
        return new PartySyncRpc(readPlayer, readIndex, readData);
    }

    public void Receive(in PartySyncRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received PartySyncRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.Party[packet._index] = packet._data;
        handled = true;
    }
}