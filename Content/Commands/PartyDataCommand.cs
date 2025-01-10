using Terramon.Helpers;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class PartyDataCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "partydata";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.PartyData.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.PartyData.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var hasValidSlot = int.TryParse(args[0], out var slot);
        if (!hasValidSlot)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Party.ParseErrorSlot"), ChatColorRed);
            return;
        }

        var hasValidSlot2 = slot is > 0 and < 7;
        if (!hasValidSlot2)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Party.SlotOutOfRange"), ChatColorRed);
            return;
        }

        var player = caller.Player.GetModPlayer<TerramonPlayer>();
        var data = player.Party[slot - 1];
        if (data == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.PartyData.NoPokemonInSlot", slot), ChatColorRed);
            return;
        }

        // Log the data to the client.log file
        Mod.Logger.Debug($"/partydata {slot} — {PrettySharp.Print(data, 1)}");

        caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.PartyData.Success", slot), ChatColorYellow);
    }
}