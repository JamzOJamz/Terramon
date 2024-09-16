using System.Collections.Generic;
using Terramon.Content.GUI;

namespace Terramon.Content.Commands;

public class QuestListCommand : DebugCommand
{

    public override CommandType Type
        => CommandType.Chat;

    public override string Command
        => "questlist";

    public override string Usage
        => "/questlist <progression/random>";

    public override string Description
        => "Lists all active quests in a certain category";
    
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        string response = "";
        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        
        if (args[0] == "random")
            foreach (var quest in player.Quests.ActiveRandQuests)
                response += $"{quest.Item1.Name}\n";
        else
            foreach (var quest in player.Quests.ActiveQuests)
                response += $"{player.Quests.GetQuest(quest.Item1).Name}\n";

        
        if (response != "")
            response = response.Substring(0, response.Length - 1);
        Main.NewTextMultiline(response, false, Color.White);
    }
}