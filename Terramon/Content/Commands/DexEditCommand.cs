using Terraria.Localization;

namespace Terramon.Content.Commands;

public class DexEditCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "dexedit";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.DexEdit.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.DexEdit.Usage");

    protected override int MinimumArgumentCount => 2;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var hasValidId = int.TryParse(args[0], out var id);
        if (!hasValidId)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.DexEdit.ParseErrorID"), ChatColorRed);
            return;
        }

        var hasValidStatus = int.TryParse(args[1], out var status);
        if (!hasValidStatus)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.DexEdit.ParseErrorStatus"), ChatColorRed);
            return;
        }

        var statusName = Enum.GetName((PokedexEntryStatus)status);
        if (statusName == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.DexEdit.StatusOutOfRange"), ChatColorRed);
            return;
        }

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var success = player.UpdatePokedex((ushort)id, (PokedexEntryStatus)status, true);
        if (success)
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.DexEdit.Success", id, statusName),
                ChatColorYellow);
        else
            caller.Reply(
                Language.GetTextValue("Mods.Terramon.Commands.DexEdit.IDOutOfRange", id, Terramon.LoadedPokemonCount),
                ChatColorRed);
    }
}