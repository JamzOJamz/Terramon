namespace Terramon.Core.Battling.BattlePackets.Messages;

public sealed class SlotChoice(byte slot) : BattleMessage
{
    public byte Slot = slot;
    public override void Write(BinaryWriter w) => w.Write(Slot);
    public override void Read(BinaryReader r) => Slot = r.ReadByte();
}
public sealed class TeamQuestion : BattleMessage;
public sealed class TeamAnswer(SimplePackedPokemon[] team) : BattleMessage
{
    public SimplePackedPokemon[] Team = team;
    public override void Write(BinaryWriter w)
    {
        w.Write((byte)Team.Length);
        for (int i = 0; i < Team.Length; i++)
            Team[i].Write(w);
    }
    public override void Read(BinaryReader r)
    {
        var length = r.ReadByte();
        Team = new SimplePackedPokemon[length];
        for (int i = 0; i < length; i++)
            Team[i] = new(r);
    }
}
public sealed class StartBattleStatement(IBattleProvider battleOwner) : BattleMessage
{
    public IBattleProvider BattleOwner = battleOwner;
    public override void Write(BinaryWriter w) => w.Write(BattleOwner.ID);
    public override void Read(BinaryReader r) => BattleOwner = r.ReadParticipant().Provider;
}
