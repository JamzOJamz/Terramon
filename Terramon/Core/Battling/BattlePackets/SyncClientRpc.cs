using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

public struct SyncClientRpc(BattleParticipant foe, ClientBattleState state) : IEasyPacket
{
    private BattleParticipant _foe = foe;
    private ClientBattleState _state = state;
    
    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_foe.Type);
        if (_foe.Type != BattleProviderType.None)
        {
            writer.Write(_foe.WhoAmI);
            writer.Write((byte)_state);
        }
    }
    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var foeType = (BattleProviderType)reader.ReadByte();
        byte foe = 0;
        if (foeType != BattleProviderType.None)
        {
            foe = reader.ReadByte();
            _state = (ClientBattleState)reader.ReadByte();
        }
        _foe = new BattleParticipant(foe, foeType);
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        var modPlayer = Main.player[sender.WhoAmI].Terramon();
        modPlayer._battleClient = new BattleClient(modPlayer)
        {
            Foe = _foe.Type == BattleProviderType.None ? null : _foe.Provider,
            State = _state,
        };
        handled = true;
    }
}

public readonly struct RequestClientRpc : IEasyPacket
{
    public void Serialise(BinaryWriter writer) { }
    
    public void Deserialise(BinaryReader reader, in SenderInfo sender) { }
    
    public void Receive(in SenderInfo sender, ref bool handled)
    {
        // Sent from remote to local client or from server to local client
        var local = BattleClient.LocalClient;
        var response = new SyncClientRpc(local.FoeID, local.State);
        var m = Terramon.Instance;
        if (sender.WhoAmI == 255)
            m.SendPacket(response);
        else
            m.SendPacket(response, sender.WhoAmI, Main.myPlayer, true);
        handled = true;
    }
}