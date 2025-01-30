using Terraria.Localization;

namespace Terramon.Content.Commands;

public class NicknameCommand : TerramonCommand
{
    /// <summary>
    ///     Maximum allowed length for a Pokémon's nickname.
    /// </summary>
    private const int MaxNicknameLength = 12;

    public override CommandType Type => CommandType.Chat;

    public override string Command => "nickname";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Nickname.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Nickname.Usage");
    
    protected override int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var activePokemonData = player.GetActivePokemon();
        if (activePokemonData == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.NoActivePokemon"), ChatColorRed);
            return;
        }

        string subcommand = args[0], nick = args.Length > 1 ? args[1] : null;
        switch (subcommand)
        {
            case "set":
                // Make sure a nickname has been provided
                if (string.IsNullOrEmpty(nick))
                {
                    caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.NoNicknameProvided"),
                        ChatColorRed);
                    return;
                }

                // Make sure the nickname is not too long (12 characters max)
                if (nick.Length > MaxNicknameLength)
                {
                    caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.NicknameTooLong"),
                        ChatColorRed);
                    return;
                }

                // Make sure the nickname is not the same as the current one
                if (activePokemonData.Nickname == nick)
                {
                    caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.SameNickname"),
                        ChatColorRed);
                    return;
                }

                // Set the nickname
                caller.Reply(string.IsNullOrEmpty(activePokemonData.Nickname)
                        ? Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.SuccessNew",
                            activePokemonData.LocalizedName, nick)
                        : Language.GetTextValue("Mods.Terramon.Commands.Nickname.Set.SuccessUpdate",
                            activePokemonData.LocalizedName, activePokemonData.Nickname, nick),
                    ChatColorYellow);
                activePokemonData.Nickname = nick;
                break;
            case "clear":
                if (string.IsNullOrEmpty(activePokemonData.Nickname))
                {
                    caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.Clear.NoNicknameSet"),
                        ChatColorRed);
                    return;
                }

                caller.Reply(
                    Language.GetTextValue("Mods.Terramon.Commands.Nickname.Clear.Success",
                        activePokemonData.LocalizedName),
                    ChatColorYellow);
                activePokemonData.Nickname = null;
                break;
            default:
                caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Nickname.InvalidSubcommand"), ChatColorRed);
                return;
        }
    }
}