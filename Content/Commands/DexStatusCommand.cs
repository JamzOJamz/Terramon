using Terramon.Core;

namespace Terramon.Content.Commands;

public class DexStatusCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "dexstatus";

    public override string Usage
        => "/dexstatus id";

    public override string Description
        => "View the status of a Pokemon in your Pokedex";

    protected override int MinimumArgumentCount => 1;

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

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var dex = player.GetPokedex();

        var hasValidId2 = new PokedexService().Entries.ContainsKey((ushort)id);
        if (!hasValidId2)
        {
            caller.Reply($"Pokédex entry {id} is out of range");
            return;
        }

        caller.Reply($"Status of Pokédex entry {id} is {PokedexEntryStatus.Search.GetName(dex.Entries[(ushort)id])}");
    }
}