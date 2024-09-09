using Terramon.Content.Configs;

namespace Terramon.Content.Commands;

public abstract class DebugCommand : TerramonCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        if (ModContent.GetInstance<GameplayConfig>().DebugMode) return;
        caller.Reply("This command requires Debug Mode to be enabled in the mod config!", Color.Red);
        Allowed = false;
    }
}