using Terramon.Content.NPCs.Pokemon;

namespace Terramon.Content.Commands;

public class PokeClearCommand : TerramonCommand
{
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "pokeclear";

    public override string Usage
        => "/pokeclear";

    public override string Description
        => "Clears all Pokémon NPCs in the world";

    protected override int MinimumArgumentCount => 0;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var clearCount = 0;
        foreach (var npc in Main.npc)
        {
            if (npc.ModNPC is not PokemonNPC) continue;
            clearCount++;
            npc.active = false;
        }

        caller.Reply($"Cleared {clearCount} Pokémon NPC(s)", new Color(255, 240, 20));
    }
}