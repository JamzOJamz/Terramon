using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;

namespace Terramon.Content.Commands;

public class NicknameCommand : TerramonCommand
{
    /// <summary>
    /// Maximum allowed length for a Pokémon's nickname.
    /// </summary>
    private const int MaxNicknameLength = 12;
    
    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "nickname";

    public override string Usage
        => "/nickname <set/clear> nickname";

    public override string Description
        => "Change the nickname of your currently active Pokémon";

    protected override int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        if (player.ActiveSlot == -1)
        {
            caller.Reply("No Pokémon is currently active");
            return;
        }
        
        var data = player.Party[player.ActiveSlot];
        string subcommand = args[0], nick = args.Length > 1 ? args[1] : null;
        switch (subcommand)
        {
            case "set":
                // Make sure the nickname is not too long (12 characters max)
                if (nick?.Length > MaxNicknameLength)
                {
                    caller.Reply("Nickname must be 12 characters or less (including spaces)");
                    return;
                }

                // Make sure the nickname is not the same as the current one
                if (data.Nickname == nick)
                {
                    caller.Reply("Nickname is already set to that value");
                    return;
                }

                // Set the nickname
                caller.Reply(data.Nickname == null
                    ? $"Set nickname of {Terramon.DatabaseV2.GetLocalizedPokemonName(data.ID)} to {nick}"
                    : $"Changed nickname of {Terramon.DatabaseV2.GetLocalizedPokemonName(data.ID)} from {data.Nickname} to {nick}");
                data.Nickname = nick;
                UILoader.GetUIState<PartyDisplay>().RecalculateSlot(player.ActiveSlot);
                break;
            case "clear":
                if (data.Nickname == null)
                {
                    caller.Reply("No nickname set for this Pokémon");
                    return;
                }

                caller.Reply($"Cleared {Terramon.DatabaseV2.GetLocalizedPokemonName(data.ID)}'s nickname");
                data.Nickname = null;
                UILoader.GetUIState<PartyDisplay>().RecalculateSlot(player.ActiveSlot);
                break;
            default:
                caller.Reply("Invalid subcommand. Use 'set' or 'clear'");
                return;
        }
    }
}