using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class TerramonItem : ModItem
{
    protected virtual bool Obtainable => true;

    protected virtual int UseRarity => ItemRarityID.Gray;

    public override void SetDefaults()
    {
        Item.rare = UseRarity;
        Item.maxStack = Item.CommonMaxStack;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        if (Obtainable) return;
        tooltips.Add(new TooltipLine(Mod, "Unobtainable",
            $"[c/ADADC6:{Language.GetTextValue("Mods.Terramon.CommonTooltips.Unobtainable")}]"));
    }
}