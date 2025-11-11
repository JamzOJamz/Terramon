using EasyPacketsLib;
using Showdown.NET.Protocol;

namespace Terramon.Core.Battling.BattlePackets;

public readonly struct BattleErrorRpc(ErrorType error, ErrorSubtype specificError)
    : IEasyPacket<BattleErrorRpc>, IEasyPacketHandler<BattleErrorRpc>
{
    private readonly ErrorType _error = error;
    private readonly ErrorSubtype _specificError = specificError;

    public void Serialise(BinaryWriter writer)
    {
        byte fullError = (byte)((byte)_error | ((byte)_specificError << 2));
        writer.Write(fullError);
    }

    public BattleErrorRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var fullError = reader.ReadByte();
        var error = (ErrorType)(fullError & 0b11);
        var specificError = (ErrorSubtype)(fullError >> 2);
        return new(error, specificError);
    }

    public void Receive(in BattleErrorRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Packet is received by a single client
        BattleSide.LocalSide.CurrentRequest = ShowdownRequest.None;
        // do stuff with error type and subtype
    }
}

public readonly struct BattleChoiceRpc(BattleChoice choice, byte operand)
    : IEasyPacket<BattleChoiceRpc>, IEasyPacketHandler<BattleChoiceRpc>
{
    private readonly BattleChoice _choice = choice;
    private readonly byte _operand = operand;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_choice);
        writer.Write(_operand);
    }

    public BattleChoiceRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var choice = (BattleChoice)reader.ReadByte();
        var operand = reader.ReadByte();
        return new(choice, operand);
    }

    public void Receive(in BattleChoiceRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Sent from client to server
        BattleManager.Instance.HandleChoice(new BattleParticipant(sender.WhoAmI, BattleProviderType.Player), packet._choice, packet._operand);
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
