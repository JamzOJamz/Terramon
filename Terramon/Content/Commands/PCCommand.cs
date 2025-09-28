using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class PCCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "pc";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.PC.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.PC.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var modPlayer = caller.Player.GetModPlayer<TerramonPlayer>();
        if (!modPlayer.HasChosenStarter)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"), ChatColorYellow);
            return;
        }
        
        // No need to open the PC if the player is already interacting with a PC
        if (modPlayer.ActivePCTileEntityID != -1) return;

        SoundEngine.PlaySound(SoundID.MenuTick);

        // If the player is not in the inventory, open it
        Main.playerInventory = true;

        // Open the PC interface
        modPlayer.ActivePCTileEntityID = int.MaxValue;
    }
}