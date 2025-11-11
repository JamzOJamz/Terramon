using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

public readonly struct ShowdownRequestRpc(ShowdownRequest request)
    : IEasyPacket<ShowdownRequestRpc>, IEasyPacketHandler<ShowdownRequestRpc>
{
    private readonly ShowdownRequest _request = request;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_request);
    }

    public ShowdownRequestRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var request = (ShowdownRequest)reader.ReadByte();
        return new(request);
    }

    public void Receive(in ShowdownRequestRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Sent from server to target client only
        handled = true;
        BattleSide.LocalSide.CurrentRequest = packet._request;
    }
}
