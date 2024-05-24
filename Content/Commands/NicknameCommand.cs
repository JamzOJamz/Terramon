using Terramon.Content.GUI;
using Terramon.Core.Loaders.UILoading;

namespace Terramon.Content.Commands;

public class NicknameCommand : TerramonCommand
{
    /// <summary>
    ///     Maximum allowed length for a Pokémon's nickname.
    /// </summary>
    private const int MaxNicknameLength = 12;

    public override CommandType Type
        => CommandType.World;

    public override string Command
        => "nickname";

    public override string Usage
        => "/nickname <set/clear> <nickname>";

    public override string Description
        => "Changes the nickname of your currently active Pokémon";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var activePokemonData = player.GetActivePokemon();
        if (activePokemonData == null)
        {
            caller.Reply("No Pokémon is currently active", Color.Red);
            return;
        }

        string subcommand = args[0], nick = args.Length > 1 ? args[1] : null;
        switch (subcommand)
        {
            case "set":
                // Make sure a nickname has been provided
                if (string.IsNullOrEmpty(nick))
                {
                    caller.Reply("No nickname provided", Color.Red);
                    return;
                }

                // Make sure the nickname is not too long (12 characters max)
                if (nick.Length > MaxNicknameLength)
                {
                    caller.Reply("Nickname must be 12 characters or less (including spaces)", Color.Red);
                    return;
                }

                // Make sure the nickname is not the same as the current one
                if (activePokemonData.Nickname == nick)
                {
                    caller.Reply("Nickname is already set to that value", Color.Red);
                    return;
                }

                // Set the nickname
                caller.Reply(string.IsNullOrEmpty(activePokemonData.Nickname)
                        ? $"Set {Terramon.DatabaseV2.GetLocalizedPokemonName(activePokemonData.ID)}'s nickname to {nick}"
                        : $"Changed {Terramon.DatabaseV2.GetLocalizedPokemonName(activePokemonData.ID)}'s nickname from {activePokemonData.Nickname} to {nick}",
                    new Color(255, 240, 20));
                activePokemonData.Nickname = nick;
                UILoader.GetUIState<PartyDisplay>().RecalculateSlot(player.ActiveSlot);
                break;
            case "clear":
                if (string.IsNullOrEmpty(activePokemonData.Nickname))
                {
                    caller.Reply("No nickname set for this Pokémon", Color.Red);
                    return;
                }

                caller.Reply($"Cleared {Terramon.DatabaseV2.GetLocalizedPokemonName(activePokemonData.ID)}'s nickname",
                    new Color(255, 240, 20));
                activePokemonData.Nickname = null;
                UILoader.GetUIState<PartyDisplay>().RecalculateSlot(player.ActiveSlot);
                break;
            default:
                caller.Reply("Invalid subcommand. Use 'set' or 'clear'", Color.Red);
                return;
        }
    }
}