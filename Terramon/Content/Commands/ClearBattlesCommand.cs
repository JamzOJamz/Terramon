using Terramon.Core.Battling.BattlePackets.Messages;
using Terraria.Chat;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public sealed class ClearBattlesCommand : DebugCommand
{
    public override string Command => "clearbattles";
    public override CommandType Type => CommandType.World;
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        new ResetEverythingStatement().Send();

        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cleared all battles for everyone"), Color.Magenta);
    }
}

public sealed class ResetEverythingStatement : BattleMessage;
