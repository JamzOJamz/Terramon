using Terramon.Content.GUI;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class DexCommand : TerramonCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "dex";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Dex.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Dex.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        if (caller.Player.Terramon().HasChosenStarter)
            HubUI.SetActive(true, false);
        else
            caller.Reply(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"), ChatColorYellow);
    }
}