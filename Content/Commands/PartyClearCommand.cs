using System;
using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;

namespace Terramon.Content.Commands;

public class PartyClearCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.Chat;

    public override string Command
        => "partyclear";

    public override string Usage
        => "/partyclear <slot>";

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
            player.ActiveSlot = -1;
            UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(player.Party);
            caller.Reply("Removed all Pokemon from the party", new Color(255, 240, 20));
            return;
        }

        var hasValidSlot = int.TryParse(args[0], out var slot);
        if (!hasValidSlot)
        {
            caller.Reply("Failed to parse slot argument as integer", Color.Red);
            return;
        }

        var hasValidSlot2 = slot is > 0 and < 7;
        if (!hasValidSlot2)
        {
            caller.Reply("Slot argument is out of range", Color.Red);
            return;
        }

        var slotIndex = slot - 1;
        if (player.Party[slotIndex] == null)
        {
            caller.Reply($"No Pokemon found in slot {slot}", Color.Red);
            return;
        }

        caller.Reply(
            $"Removed {player.Party[slotIndex].DisplayName} from the party", new Color(255, 240, 20));
        player.Party[slotIndex] = null;
        for (var i = slotIndex + 1; i < player.Party.Length; i++) player.Party[i - 1] = player.Party[i];
        player.Party[5] = null;
        if (player.ActiveSlot == slotIndex) player.ActiveSlot = -1;
        UILoader.GetUIState<PartyDisplay>().UpdateAllSlots(player.Party);
    }
}