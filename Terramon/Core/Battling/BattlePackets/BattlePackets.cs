using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

/// <summary>
///     Packet for battle setup. Is sent from client to the server
///     This is separate from sending team packet as to avoid sending unnecessary data if the battle ends up cancelled
///     Not sent for wild Pokémon. Those always pick 0 obviously
/// </summary>
public readonly struct BattlePickRpc(byte chosenSlot)
    : IEasyPacket<BattlePickRpc>, IEasyPacketHandler<BattlePickRpc>
{
    private readonly byte _chosenSlot = chosenSlot;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_chosenSlot);
    }

    public BattlePickRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var chosenSlot = reader.ReadByte();
        return new(chosenSlot);
    }

    public void Receive(in BattlePickRpc packet, in SenderInfo sender, ref bool handled)
    {
        this.DebugLog();
        // This packet is sent to the server only
        handled = true;
        // Handle picking
        BattleManager.Instance.SetPick(sender.WhoAmI, packet._chosenSlot);
    }
}

/// <summary>
///     Not sent for Pokemon NPCs since team is determined immediately
/// </summary>
public readonly struct BattleTeamRequestRpc()
    : IEasyPacket<BattleTeamRequestRpc>, IEasyPacketHandler<BattleTeamRequestRpc>
{
    public void Serialise(BinaryWriter writer) { }
    public BattleTeamRequestRpc Deserialise(BinaryReader reader, in SenderInfo sender) => new();
    public void Receive(in BattleTeamRequestRpc packet, in SenderInfo sender, ref bool handled)
    {
        this.DebugLog();
        // This packet is sent from the server to the pair of clients in a battle who've just finished picking their starting Pokémon,
        // so those clients are the ones receiving it
        handled = true;
        var response = new BattleTeamRpc(TerramonPlayer.LocalPlayer.GetNetTeam());
        Terramon.Instance.SendPacket(in response);
    }
}

/// <summary>
///     Not sent for Pokemon NPCs since team is determined immediately
/// </summary>
public readonly struct BattleTeamRpc(SimplePackedPokemon[] packedMon)
    : IEasyPacket<BattleTeamRpc>, IEasyPacketHandler<BattleTeamRpc>
{
    private readonly SimplePackedPokemon[] _packedMon = packedMon;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_packedMon.Length);
        for (int i = 0; i < _packedMon.Length; i++)
            _packedMon[i].Write(writer);
    }

    public BattleTeamRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var packedMon = new SimplePackedPokemon[reader.ReadByte()];
        for (int i = 0; i < packedMon.Length; i++)
            packedMon[i] = new(reader);

        return new BattleTeamRpc(packedMon);
    }

    public void Receive(in BattleTeamRpc packet, in SenderInfo sender, ref bool handled)
    {
        handled = true;
        this.DebugLog();

        if (!Main.dedServ)
            throw new Exception($"Somehow, a {nameof(BattleTeamRpc)} packet was received on the client");

        BattleManager.Instance.SetTeam(sender.WhoAmI, packet._packedMon);
    }
}

public readonly struct BattleStartRpc(BattleParticipant whoStarted, BattleParticipant other)
    : IEasyPacket<BattleStartRpc>, IEasyPacketHandler<BattleStartRpc>
{
    private readonly BattleParticipant _whoStarted = whoStarted;
    private readonly BattleParticipant _other = other;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_whoStarted);
        writer.Write(_other);
    }

    public BattleStartRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var whoStarted = reader.ReadParticipant();
        var other = reader.ReadParticipant();
        return new(whoStarted, other);
    }

    public void Receive(in BattleStartRpc packet, in SenderInfo sender, ref bool handled)
    {
        // This packet is received by every client
        handled = true;
        this.DebugLog();
        if (Main.dedServ)
            throw new Exception($"Somehow, a {nameof(BattleStartRpc)} packet was received on the server");

        var clientA = packet._whoStarted.Client;
        var clientB = packet._other.Client;
        clientA.State = ClientBattleState.Ongoing;
        clientB.State = ClientBattleState.Ongoing;
        clientA.Provider.StartBattleEffects();
        clientB.Provider.StartBattleEffects();

        var battle = new BattleField()
        { 
            A = new(clientA.Provider),
            B = new(clientB.Provider),
        };
        clientA.Battle = battle;
        clientB.Battle = battle;

        Main.NewText($"Battle started by {clientA.Name} against {clientB.Name}!", Color.Aqua);
    }
}

public enum BattleOutcome : byte
{
    Win,
    AgreedWin,
    Tie,
    AgreedTie,
    ForcedTie,
}

public readonly struct EndBattleRpc(BattleParticipant winner, BattleOutcome outcome)
    : IEasyPacket<EndBattleRpc>, IEasyPacketHandler<EndBattleRpc>
{
    // with ties, this will just be the battle owner instead
    private readonly BattleParticipant _winner = winner;
    private readonly BattleOutcome _outcome = outcome;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_winner);
        writer.Write((byte)_outcome);
    }

    public EndBattleRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var winner = reader.ReadParticipant();
        var outcome = (BattleOutcome)reader.ReadByte();
        return new(winner, outcome);
    }

    public void Receive(in EndBattleRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Sent from server to all clients
        handled = true;
        this.DebugLog();

        var a = BattleManager.GetClient(packet._winner);
        var b = a.Foe.BattleClient;

        a.BattleStopped();
        b.BattleStopped();

        EndMessage(packet._outcome, a, b);
    }

    public static void EndMessage(BattleOutcome outcome, BattleClient a, BattleClient b)
    {
        var endMsg = outcome switch
        {
            BattleOutcome.Win => "{0} defeated {1}!",
            BattleOutcome.AgreedWin => "{1} forfeited their battle against {0}!",
            BattleOutcome.Tie => "{0} and {1}'s battle ended in a tie!",
            BattleOutcome.AgreedTie => "{0} and {1} agreed to a tie!",
            BattleOutcome.ForcedTie => "{0} and {1}'s battle was forcibly ended!",
            _ => throw new Exception()
        };

        var formatted = string.Format(endMsg, a.Name, b.Name);
        var msgColor =
            outcome < BattleOutcome.Tie ? Color.Green :
            outcome != BattleOutcome.ForcedTie ? Color.Yellow :
            Color.Red;

        Main.NewText(formatted, msgColor);
    }
}

public readonly struct EndBattleRequestRpc(bool resign) // as opposed to tie request
    : IEasyPacket<EndBattleRequestRpc>, IEasyPacketHandler<EndBattleRequestRpc>
{
    private readonly bool _resign = resign;

    public void Serialise(BinaryWriter writer)
    {
        writer.Write(_resign);
    }

    public EndBattleRequestRpc Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        var resign = reader.ReadBoolean();
        return new(resign);
    }

    public void Receive(in EndBattleRequestRpc packet, in SenderInfo sender, ref bool handled)
    {
        // Sent from client to server if resign
        // Sent from client to other client through forwarding if tie request
        handled = true;
        this.DebugLog();

        if (Main.dedServ)
            BattleManager.Instance.SubmitEndRequest(sender.WhoAmI, packet._resign);
        else if (!packet._resign)
            Main.NewText($"{Main.player[sender.WhoAmI].name} asked to agree to a tie");
    }
}
