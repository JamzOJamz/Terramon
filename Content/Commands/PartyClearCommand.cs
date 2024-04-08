using Terramon.Content.GUI;
using Terramon.Core.Helpers;
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
        => "Displays info for the specified Pokémon in your party";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        var player = caller.Player.GetModPlayer<TerramonPlayer>();

        if (args[0] == "all")
        {
            for (int i = 0; i < 6; i++)
            {
                player.Party[i] = null;
            }
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

        if (player.Party[slot - 1] == null)
        {
            caller.Reply($"No Pokemon found in slot {slot}");
            return;
        }

        caller.Reply($"Removed {Terramon.DatabaseV2.GetLocalizedPokemonName(player.Party[slot - 1].ID)} from the party");
        player.Party[slot - 1] = null;
        for (int i = slot - 1; i < 5; i++)
        {
            player.SwapParty(i + 1, i);
            UILoader.GetUIState<PartyDisplay>().UpdateSlot(player.Party[i], i);
        }
        UILoader.GetUIState<PartyDisplay>().UpdateSlot(player.Party[5], 5);
    }
}