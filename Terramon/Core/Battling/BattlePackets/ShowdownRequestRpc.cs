namespace Terramon.Core.Battling.BattlePackets;

public struct ShowdownRequestRpc(ShowdownRequest request) : IEasyPacket
{
    private ShowdownRequest _request = request;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_request);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _request = (ShowdownRequest)reader.ReadByte();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        // Sent from server to target client only
        handled = true;
        BattleSide.LocalSide.CurrentRequest = _request;
    }
}
