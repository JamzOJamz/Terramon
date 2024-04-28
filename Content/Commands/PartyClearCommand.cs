using System;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;

namespace Terramon.Content.Commands;

public class PartyClearCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "partyclear";

    public override string Usage
        => "/partyclear slot";

    public override string Description
        => "Removes the specified Pokémon from your party";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        var player = caller.Player.GetModPlayer<TerramonPlayer>();

        if (args[0] == "all")
        {
            Array.Clear(player.Party, 0, player.Party.Length);
            UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(player.Party);
            caller.Reply("Removed all Pokemon from the party");
            return;
        }

        var hasValidSlot = int.TryParse(args[0], out var slot);
        if (!hasValidSlot)
        {
            caller.Reply("Failed to parse slot argument as integer");
            return;
        }

        var hasValidSlot2 = slot is > 0 and < 7;
        if (!hasValidSlot2)
        {
            caller.Reply("Slot argument is out of range");
            return;
        }

        var slotIndex = slot - 1;
        if (player.Party[slotIndex] == null)
        {
            caller.Reply($"No Pokemon found in slot {slot}");
            return;
        }

        caller.Reply(
            $"Removed {player.Party[slotIndex].DisplayName} from the party");
        player.Party[slotIndex] = null;
        for (var i = slotIndex + 1; i < player.Party.Length; i++) player.Party[i - 1] = player.Party[i];
        player.Party[5] = null;
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(player.Party);
    }
}