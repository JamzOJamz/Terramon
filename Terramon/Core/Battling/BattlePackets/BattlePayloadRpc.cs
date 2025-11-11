using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;
public readonly struct BattlePayloadRpc(BattleParticipant battleOwner, MemoryStream buffer)
    : IEasyPacket<BattlePayloadRpc>, IEasyPacketHandler<BattlePayloadRpc>
{
    private readonly BattleParticipant _battleOwner = battleOwner;
    private readonly MemoryStream _buffer = buffer;
    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_battleOwner);
        writer.Write((int)_buffer.Length);
        _buffer.WriteTo(writer.BaseStream);
    }

    public BattlePayloadRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var battleOwner = reader.ReadParticipant();
        var length = reader.ReadInt32();
        var buffer = new MemoryStream(reader.ReadBytes(length));
        return new(battleOwner, buffer);
    }

    public void Receive(in BattlePayloadRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Being sent from server to clients
        this.DebugLog();
        handled = true;

        Main.NewText($"Received payload with {packet._buffer.Length} bytes of content!");

        using var reader = new BinaryReader(packet._buffer);
        var o = packet._battleOwner.Client.Battle.Observer;
        o.Receive(reader);
    }
}
