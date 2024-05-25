using System.IO;
using Terramon.Core.Networking;
using Terraria.ID;

namespace Terramon.Content.Packets;

public readonly struct PartySyncPacket(byte player, byte index, PokemonData data)
    : IEasyPacket<PartySyncPacket>, IEasyPacketHandler<PartySyncPacket>
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

    public PartySyncPacket Deserialize(BinaryReader reader, in SenderInfo sender)
    {
        var readPlayer = reader.ReadByte();
        var readIndex = reader.ReadByte();
        var readData = reader.ReadBoolean() ? new PokemonData().NetRead(reader) : null;
        return new PartySyncPacket(readPlayer, readIndex, readData);
    }

    public void Receive(in PartySyncPacket packet, in SenderInfo sender, ref bool handled)
    {
        /*sender.Mod.Logger.Debug(
            $"Received PartySyncPacket on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");*/
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.Party[packet._index] = packet._data;
        if (Main.netMode == NetmodeID.Server && sender.Forwarded)
            // Forward the changes to the other clients
            sender.Mod.SendPacket(packet, ignoreClient: sender.WhoAmI);
        handled = true;
    }
}