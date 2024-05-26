using System.IO;
using Terramon.Core.Networking;
using Terraria.ID;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing a player's active Pokémon with all clients.
/// </summary>
public readonly struct SetActivePokemonRpc(byte player, PokemonData data)
    : IEasyPacket<SetActivePokemonRpc>, IEasyPacketHandler<SetActivePokemonRpc>
{
    private readonly byte _player = player;
    private readonly PokemonData _data = data;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_data != null);
        _data?.NetWrite(writer);
    }

    public SetActivePokemonRpc Deserialize(BinaryReader reader, in SenderInfo sender)
    {
        var readPlayer = reader.ReadByte();
        var readData = reader.ReadBoolean() ? new PokemonData().NetRead(reader) : null;
        return new SetActivePokemonRpc(readPlayer, readData);
    }

    public void Receive(in SetActivePokemonRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SetActivePokemonRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        player.Party[0] = packet._data;
        player.ActiveSlot = packet._data != null ? 0 : -1;
        handled = true;
    }
}