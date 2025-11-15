namespace Terramon.Core.Battling.BattlePackets;
public struct BattlePayloadRpc(IBattleProvider battleOwner, MemoryStream buffer) : IEasyPacket
{
    private IBattleProvider _battleOwner = battleOwner;
    private MemoryStream _buffer = buffer;
    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_battleOwner);
        writer.Write((ushort)_buffer.Length);
        _buffer.WriteTo(writer.BaseStream);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _battleOwner = reader.ReadParticipant();
        var length = reader.ReadUInt16();
        _buffer = new MemoryStream(reader.ReadBytes(length));
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        // Being sent from server to clients
        this.ReceiveLog();
        handled = true;

        using var reader = new BinaryReader(_buffer);
        var c = _battleOwner.BattleClient;
        var o = c.Battle;
        o.Receive(reader);
    }
}
