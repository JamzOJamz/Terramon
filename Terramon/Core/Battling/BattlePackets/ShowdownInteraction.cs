using Showdown.NET.Protocol;

namespace Terramon.Core.Battling.BattlePackets;

public struct BattleErrorRpc(ErrorType error, ErrorSubtype specificError)
    : IEasyPacket
{
    private ErrorType _error = error;
    private ErrorSubtype _specificError = specificError;

    public readonly void Serialise(BinaryWriter writer)
    {
        byte fullError = (byte)((byte)_error | ((byte)_specificError << 2));
        writer.Write(fullError);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var fullError = reader.ReadByte();
        _error = (ErrorType)(fullError & 0b11);
        _specificError = (ErrorSubtype)(fullError >> 2);
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        // Packet is received by a single client
        BattleClient.LocalClient.CurrentRequest = ShowdownRequest.None;
        // do stuff with error type and subtype
    }
}

public struct BattleChoiceRpc(BattleChoice choice, byte operand) : IEasyPacket
{
    private BattleChoice _choice = choice;
    private byte _operand = operand;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_choice);
        writer.Write(_operand);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _choice = (BattleChoice)reader.ReadByte();
        _operand = reader.ReadByte();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        // Sent from client to server
        BattleManager.Instance.HandleChoice(new BattleParticipant(sender.WhoAmI, BattleProviderType.Player), _choice, _operand);
    }
}

[Flags]
public enum BattleChoice : byte
{
    Default = 0,
    Pass = 1,
    Move = 2,
    Switch = 4,
    Mega = 8,
    ZMove = 16,
    Max = 32,
}
