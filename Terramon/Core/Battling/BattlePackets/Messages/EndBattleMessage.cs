namespace Terramon.Core.Battling.BattlePackets.Messages;

public sealed class WinStatement(IBattleProvider winner) : BattleMessage
{
    public IBattleProvider Winner = winner;
    public override void Write(BinaryWriter w) => w.Write(Winner.ID);
    public override void Read(BinaryReader r) => Winner = r.ReadParticipant().Provider;
}
public sealed class ForfeitOrder : BattleMessage;
public sealed class ForfeitStatement(IBattleProvider forfeiter) : BattleMessage
{
    public IBattleProvider Forfeiter = forfeiter;
    public override void Write(BinaryWriter w) => w.Write(Forfeiter.ID);
    public override void Read(BinaryReader r) => Forfeiter = r.ReadParticipant().Provider;
}
public sealed class TieQuestion : BattleMessage;
public sealed class TieTakeback : BattleMessage;
public sealed class TieStatement(IBattleProvider eitherParticipant, TieStatement.TieType type) : BattleMessage
{
    public enum TieType : byte
    {
        Regular,
        Agreed,
        Forced,
    }
    public IBattleProvider EitherParticipant = eitherParticipant;
    public TieType Type = type;
    public override void Write(BinaryWriter w)
    {
        w.Write(EitherParticipant.ID);
        w.Write((byte)Type);
    }
    public override void Read(BinaryReader r)
    {
        EitherParticipant = r.ReadParticipant().Provider;
        Type = (TieType)r.ReadByte();
    }
}
