using Terraria.Localization;

namespace Terramon.Content.Commands;

public class PCCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "pc";
    
    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.PC.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.PC.Usage");
}