using Terramon.Content.Configs;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public abstract class DebugCommand : TerramonCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        if (ModContent.GetInstance<GameplayConfig>().DebugMode) return;
        caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.RequiresDebugMode"), ChatColorRed);
        Allowed = false;
    }
}