using System.Collections.Generic;
using Terraria.Localization;

namespace Terramon.Content.Items;

[Autoload(false)]
public abstract class TerramonItem : ModItem
{
    /// <summary>
    ///     Indicates whether this item is legitimately obtainable in-game. If false, the item will have a tooltip
    ///     indicating it is unobtainable.
    /// </summary>
    public virtual bool Obtainable => true;

    /// <summary>
    ///     The rarity of the item. Defaults to White.
    /// </summary>
    protected virtual int UseRarity => ItemRarityID.White;

    public override string Texture => "ModLoader/UnloadedItem"; // Default texture

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
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