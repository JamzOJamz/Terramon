using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;

namespace Terramon.Content.Items;

[Autoload(false)]
public abstract class TerramonItem : ModItem
{
    /// <summary>
    ///     The loading priority of this type. Lower values are loaded first.
    /// </summary>
    public virtual ItemLoadPriority LoadPriority => ItemLoadPriority.Unspecified;

    /// <summary>
    ///     Whether this item is legitimately obtainable in-game. If false, the item will have a tooltip indicating it is
    ///     unobtainable.
    /// </summary>
    protected virtual bool Obtainable => true;

    /// <summary>
    ///     The rarity of the item. Defaults to White.
    /// </summary>
    protected virtual int UseRarity => ItemRarityID.White;

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

public enum ItemLoadPriority
{
    PokeBalls,
    Apricorns,
    EvolutionaryItems,
    Vitamins,
    KeyItems,
    PokeBallMinis,
    Unspecified
}