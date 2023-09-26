using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.Localization;

namespace Terramon.Content.Items.Evolutionary;

public abstract class EvolutionaryItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/Evolutionary/" + GetType().Name;
    
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 50000;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "EvolutionaryItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.EvolutionaryItem")));
    }
}