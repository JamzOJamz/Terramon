using Terramon.Content.GUI;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class StarterCommand : TerramonCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "starter";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Starter.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Starter.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);

        if (!caller.Player.Terramon().HasChosenStarter)
            StarterSelectUI.Show(playSound: false);
        else caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Starter.AlreadyChosen"), ChatColorYellow);
    }
}

/// <summary>
///     Alias for <see cref="StarterCommand" />.
/// </summary>
public class ChooseCommand : StarterCommand
{
    public override string Command => "choose"; 
    
    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Choose.Description");
}