using System;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class PartyClearCommand : DebugCommand
{
    public override CommandType Type
        => CommandType.Chat;

    public override string Command
        => "partyclear";

    public override string Description
        => Language.GetTextValue("Mods.Terramon.Commands.PartyClear.Description");

    public override string Usage
        => Language.GetTextValue("Mods.Terramon.Commands.PartyClear.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        var player = caller.Player.GetModPlayer<TerramonPlayer>();

        if (args[0] == "all")
        {
            Array.Clear(player.Party, 0, player.Party.Length);
            player.ActiveSlot = -1;
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.PartyClear.SuccessAll"), ChatColorYellow);
            return;
        }

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

        var slotIndex = slot - 1;
        if (player.Party[slotIndex] == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.PartyClear.NoPokemonInSlot", slot),
                ChatColorRed);
            return;
        }

        caller.Reply(
            Language.GetTextValue("Mods.Terramon.Commands.PartyClear.Success", player.Party[slotIndex].DisplayName),
            ChatColorYellow);
        player.Party[slotIndex] = null;
        for (var i = slotIndex + 1; i < player.Party.Length; i++) player.Party[i - 1] = player.Party[i];
        player.Party[5] = null;
        if (player.ActiveSlot == slotIndex) player.ActiveSlot = -1;
        else if (slotIndex < player.ActiveSlot) player.ActiveSlot--;
    }
}