namespace Terramon.Content.Packets;

/// <summary>
///     A packet for synchronizing a player's active Pokémon with all clients.
/// </summary>
public struct UpdateActivePokemonRpc(
    byte player,
    PokemonData data,
    int syncFields = PokemonData.AllFieldsBitmask)
    : IEasyPacket
{
    private byte _player = player;
    private PokemonData _data = data;
    private int _syncFields = syncFields;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        var hasData = _data != null;
        writer.Write(hasData);
        if (!hasData)
            return;
        writer.Write7BitEncodedInt(_syncFields);
        _data?.NetWrite(writer, _syncFields);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _player = reader.ReadByte();
        if (!reader.ReadBoolean())
            return;
        _syncFields = reader.Read7BitEncodedInt();
        _data = new PokemonData().NetRead(reader);
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        sender.Mod.Logger.Debug(
            $"Received SetActivePokemonRpc on {(Main.netMode == NetmodeID.Server ? "server" : "client")} for player {_player}");
        var player = Main.player[_player].GetModPlayer<TerramonPlayer>();
        if (_data == null)
        {
            player.Party[0] = null;
            player.ActiveSlot = -1;
        }
        else
        {
            player.Party[0] ??= new PokemonData();
            _data.CopyNetStateTo(player.Party[0], _syncFields);
            player.ActiveSlot = 0;
        }

        handled = true;
    }
}