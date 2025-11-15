namespace Terramon.Core.Battling.BattlePackets.Messages;

public sealed class ChallengeQuestion : BattleMessage;
public sealed class ChallengeTakeback : BattleMessage;
public sealed class ChallengeError : BattleMessage;
public sealed class ChallengeAnswer : BattleMessage
{
    public bool Yes;
    public ChallengeAnswer() { }
    public ChallengeAnswer(bool yes) => Yes = yes;
    public override void Write(BinaryWriter w) => w.Write(Yes);
    public override void Read(BinaryReader r) => Yes = r.ReadBoolean();
}
