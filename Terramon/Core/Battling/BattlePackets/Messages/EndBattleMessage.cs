namespace Terramon.Core.Battling.BattlePackets.Messages;

public sealed class WinStatement : BattleMessage
{
    public IBattleProvider Winner;
    public WinStatement() { }
    public WinStatement(IBattleProvider winner) => Winner = winner;
    public override void Write(BinaryWriter w) => w.Write(Winner);
    public override void Read(BinaryReader r) => Winner = r.ReadParticipant();
}
public sealed class ForfeitOrder : BattleMessage;
public sealed class ForfeitStatement : BattleMessage
{
    public IBattleProvider Forfeiter;
    public ForfeitStatement() { }
    public ForfeitStatement(IBattleProvider forfeiter) => Forfeiter = forfeiter;
    public override void Write(BinaryWriter w) => w.Write(Forfeiter);
    public override void Read(BinaryReader r) => Forfeiter = r.ReadParticipant();
}
public sealed class TieQuestion : BattleMessage;
public sealed class TieTakeback : BattleMessage;
public sealed class TieStatement : BattleMessage
{
    public enum TieType : byte
    {
        Regular,
        Agreed,
        Forced,
    }
    public IBattleProvider EitherParticipant;
    public TieType Type;
    public TieStatement() { }
    public TieStatement(IBattleProvider eitherParticipant, TieType type)
    {
        EitherParticipant = eitherParticipant;
        Type = type;
    }
    public override void Write(BinaryWriter w)
    {
        w.Write(EitherParticipant);
        w.Write((byte)Type);
    }
    public override void Read(BinaryReader r)
    {
        EitherParticipant = r.ReadParticipant();
        Type = (TieType)r.ReadByte();
    }
}
