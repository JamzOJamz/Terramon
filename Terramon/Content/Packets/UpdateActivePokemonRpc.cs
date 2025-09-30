using EasyPacketsLib;

namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing a player's active Pokémon with all clients.
/// </summary>
public readonly struct UpdateActivePokemonRpc(
    byte player,
    PokemonData data,
    int syncFields = PokemonData.AllFieldsBitmask)
    : IEasyPacket<UpdateActivePokemonRpc>, IEasyPacketHandler<UpdateActivePokemonRpc>
{
    private readonly byte _player = player;
    private readonly PokemonData _data = data;
    private readonly int _syncFields = syncFields;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        var hasData = _data != null;
        writer.Write(hasData);
        if (!hasData)
            return;
        writer.Write7BitEncodedInt(_syncFields);
        _data?.NetWrite(writer, _syncFields);
    }

    public UpdateActivePokemonRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var readPlayer = reader.ReadByte();
        if (!reader.ReadBoolean())
            return new UpdateActivePokemonRpc(readPlayer, null);
        var readSyncFields = reader.Read7BitEncodedInt();
        var readData = new PokemonData().NetRead(reader);
        return new UpdateActivePokemonRpc(readPlayer, readData, readSyncFields);
    }

    public void Receive(in UpdateActivePokemonRpc packet, in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SetActivePokemonRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {packet._player}");
        var player = Main.player[packet._player].GetModPlayer<TerramonPlayer>();
        if (packet._data == null)
        {
            player.Party[0] = null;
            player.ActiveSlot = -1;
        }
        else
        {
            player.Party[0] ??= new PokemonData();
            packet._data.CopyNetStateTo(player.Party[0], packet._syncFields);
            player.ActiveSlot = 0;
        }

        handled = true;
    }
}