namespace Terramon.Content.Commands;

public class DexEditCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "dexedit";

    public override string Usage
        => "/dexedit id status";

    public override string Description
        => "Forcefully set Pokédex entry statuses";

    protected override int MinimumArgumentCount => 2;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var hasValidId = int.TryParse(args[0], out var id);
        if (!hasValidId)
        {
            caller.Reply("Failed to parse ID argument as integer");
            return;
        }

        var hasValidStatus = int.TryParse(args[1], out var status);
        if (!hasValidStatus)
        {
            caller.Reply("Failed to parse status argument as integer");
            return;
        }

        var hasValidStatus2 = PokedexEntryStatus.Search.TryGetName(status, out var statusName);
        if (!hasValidStatus2)
        {
            caller.Reply("Status argument is out of range");
            return;
        }

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var success = player.UpdatePokedex((ushort)id, (byte)status);
        caller.Reply(success
            ? $"Successfully set Pokédex entry {id} to status {statusName}"
            : $"Pokédex entry {id} is out of range");
    }
}