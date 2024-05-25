using Terramon.Helpers;

namespace Terramon.Content.Commands;

public class PartyDataCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "partydata";

    public override string Usage
        => "/partydata <slot>";

    public override string Description
        => "Logs info for the specified Pokémon in your party";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

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

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var data = player.Party[slot - 1];
        if (data == null)
        {
            caller.Reply($"No Pokémon data available for slot {slot}");
            return;
        }
        
        // Log the data to the client.log file
        Mod.Logger.Debug($"/partydata {slot} — {PrettySharp.Print(data, 1)}");

        caller.Reply("Pokemon data for slot " + slot + " written to client.log");
    }
}