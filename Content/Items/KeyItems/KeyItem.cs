using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.Localization;

namespace Terramon.Content.Items.KeyItems;

public abstract class KeyItem : TerramonItem
{
    public override string Texture => "Terramon/Assets/Items/KeyItems/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "KeyItem", Language.GetTextValue("Mods.Terramon.CommonTooltips.KeyItem")));
    }
}