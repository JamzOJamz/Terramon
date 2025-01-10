using Terraria.Localization;

namespace Terramon.Core;

public class TerramonGlobalItem : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (TerramonItemAPI.Sets.Unobtainable.Contains(item.type))
            tooltips.Add(new TooltipLine(Mod, "Unobtainable",
                $"[c/ADADC6:{Language.GetTextValue("Mods.Terramon.CommonTooltips.Unobtainable")}]"));
    }
}