using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

public readonly struct SyncClientRpc(BattleParticipant foe, ClientBattleState state)
    : IEasyPacket<SyncClientRpc>, IEasyPacketHandler<SyncClientRpc>
{
    private readonly BattleParticipant _foe = foe;
    private readonly ClientBattleState _state = state;
    public void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_foe.Type);
        if (_foe.Type != BattleProviderType.None)
        {
            writer.Write(_foe.WhoAmI);
            writer.Write((byte)_state);
        }
    }
    public SyncClientRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var foeType = (BattleProviderType)reader.ReadByte();
        var foe = (byte)0;
        var state = ClientBattleState.None;
        if (foeType != BattleProviderType.None)
        {
            foe = reader.ReadByte();
            state = (ClientBattleState)reader.ReadByte();
        }
        return new(new(foe, foeType), state);
    }

    public void Receive(in SyncClientRpc packet, in SenderInfo sender, ref bool handled)
    {
        var modPlayer = Main.player[sender.WhoAmI].Terramon();
        modPlayer._battleClient = new(modPlayer)
        {
            Foe = packet._foe.Type == BattleProviderType.None ? null : packet._foe.Provider,
            State = packet._state,
        };
        handled = true;
    }
}

public readonly struct RequestClientRpc()
    : IEasyPacket<RequestClientRpc>, IEasyPacketHandler<RequestClientRpc>
{
    public void Serialise(BinaryWriter writer) { }
    public RequestClientRpc Deserialise(BinaryReader reader, in SenderInfo sender) => new();
    public void Receive(in RequestClientRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Sent from remote to local client or from server to local client
        var local = BattleClient.LocalClient;
        var response = new SyncClientRpc(local.FoeID, local.State);
        var m = Terramon.Instance;
        if (sender.WhoAmI == 255)
            m.SendPacket(in response);
        else
            m.SendPacket(in response, sender.WhoAmI, Main.myPlayer, true);
        handled = true;
    }
}