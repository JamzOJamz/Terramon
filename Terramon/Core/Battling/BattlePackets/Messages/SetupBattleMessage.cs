namespace Terramon.Core.Battling.BattlePackets.Messages;

public sealed class SlotChoice : BattleMessage
{
    public byte Slot;
    public SlotChoice() { }
    public SlotChoice(byte slot) => Slot = slot;
    public override void Write(BinaryWriter w) => w.Write(Slot);
    public override void Read(BinaryReader r) => Slot = r.ReadByte();
}
public sealed class TeamQuestion : BattleMessage;
public sealed class TeamAnswer : BattleMessage
{
    public SimplePackedPokemon[] Team;
    public TeamAnswer() { }
    public TeamAnswer(SimplePackedPokemon[] team) => Team = team;
    public override void Write(BinaryWriter w)
    {
        w.Write((byte)Team.Length);
        for (var i = 0; i < Team.Length; i++)
            Team[i].Write(w);
    }
    public override void Read(BinaryReader r)
    {
        var length = r.ReadByte();
        Team = new SimplePackedPokemon[length];
        for (var i = 0; i < length; i++)
            Team[i] = new(r);
    }
}
public sealed class StartBattleStatement : BattleMessage
{
    public IBattleProvider BattleOwner;
    public StartBattleStatement() { }
    public StartBattleStatement(IBattleProvider battleOwner) => BattleOwner = battleOwner;
    public override void Write(BinaryWriter w) => w.Write(BattleOwner);
    public override void Read(BinaryReader r) => BattleOwner = r.ReadParticipant();
}
